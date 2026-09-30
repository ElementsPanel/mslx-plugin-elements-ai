using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;
using MSLX.SDK.IServices;
using MSLX.SDK.Models.Files;

static class TaskProgressTests
{
    private static readonly UserContext User = new("owner", false, "scope");

    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("task snapshots preserve identity and terminal states", async () =>
        {
            var manager = new FakeTaskManager();
            var service = new TaskProgressService(manager);
            var (id, _) = service.StartDownload(User, 42, "mod.jar");
            var pending = service.Read(id, User);
            Check(pending.State == "pending" && !pending.Completed && pending.InstanceId == 42 && pending.FileName == "mod.jar", "pending task lost");
            service.Update(id, 125, "download");
            Check(service.Read(id, User).Value == 100 && !service.Read(id, User).Completed, "100% prematurely marked success");
            foreach (var state in new[] { TaskState.Success, TaskState.Failed, TaskState.Canceled })
            {
                manager.UpdateProgress(id, 43, "result", state);
                var result = await service.WaitAsync(id, User, 1, null, default);
                Check(result.Completed && !result.WaitTimedOut && result.Success == (state == TaskState.Success), "terminal state incorrect");
                if (state == TaskState.Success) Check(result.Task.Value == 100, "success percentage incorrect");
            }
        });
        yield return ("waiting streams updates and sees eventual completion", async () =>
        {
            var service = new TaskProgressService(new FakeTaskManager());
            var (id, _) = service.StartDownload(User, 1, "mod.jar");
            var updates = new List<AiTaskProgress>();
            var result = await service.WaitAsync(id, User, 2, progress =>
            {
                updates.Add(progress);
                if (!progress.Completed) service.Complete(id, "installed");
                return Task.CompletedTask;
            }, default);
            Check(result.Completed && result.Success && updates.Count == 2 && !updates[0].Completed && updates[1].Success, "wait missed task transition");
        });
        yield return ("wait timeout and cancellation leave background tasks running", async () =>
        {
            var manager = new FakeTaskManager();
            var service = new TaskProgressService(manager);
            var (id, token) = service.StartDownload(User, 1, "mod.jar");
            service.Update(id, 40, "downloading");
            var result = await service.WaitAsync(id, User, 1, null, default);
            Check(result.WaitTimedOut && !result.Completed && !result.Success, "timeout reported success");
            using var cancel = new CancellationTokenSource(30);
            await Throws<OperationCanceledException>(() => service.WaitAsync(id, User, 60, null, cancel.Token));
            Check(!token.IsCancellationRequested && manager.Cancellations == 0 && service.Read(id, User).State == "running", "wait canceled background job");
        });
        yield return ("task access requires ownership and validates wait bounds", async () =>
        {
            var service = new TaskProgressService(new FakeTaskManager());
            var (id, _) = service.StartDownload(User, 1, "mod.jar");
            foreach (var other in new[] { new UserContext("other", false, "scope"), new UserContext("admin", true, "scope") })
                await Throws<ToolException>(() => service.WaitAsync(id, other, 1, null, default));
            foreach (var bad in new[] { "../tasks", new string('x', 32), Guid.NewGuid().ToString("N") })
                await Throws<ToolException>(() => service.WaitAsync(bad, User, 1, null, default));
            foreach (var timeout in new[] { 0, 301 })
                await Throws<ToolException>(() => service.WaitAsync(id, User, timeout, null, default));
        });
        yield return ("wait tool returns receipts and is available without mutation approval", async () =>
        {
            var service = new TaskProgressService(new FakeTaskManager());
            var (id, _) = service.StartDownload(User, 1, "mod.jar");
            service.Fail(id, "network error");
            var tools = new MslxToolService(null!, null!, null!, User, new(null, null, null), null!, service);
            using var args = JsonDocument.Parse(JsonSerializer.Serialize(new { taskId = id, timeoutSeconds = 1 }));
            var result = await tools.ExecuteAsync("wait_for_task", args.RootElement, null, default);
            Check(!result.Ok && result.TaskProgress?.State == "failed" && result.Value is TaskWaitResult { Completed: true, Success: false }, "failed wait receipt lost");
            Check(MslxToolService.Definitions(false).ToJsonString().Contains("wait_for_task") && !MslxToolService.SensitiveTools.Contains("wait_for_task"), "wait permission incorrect");
        });
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}

sealed class FakeTaskManager : IBackgroundTaskManager
{
    private readonly Dictionary<string, BackgroundTaskItem> items = new();
    public int Cancellations { get; private set; }
    public (BackgroundTaskItem task, CancellationToken token) CreateTask(string userId, uint instanceId, TaskType type, string title, string targetName)
    {
        var task = new BackgroundTaskItem { UserId = userId, InstanceId = instanceId, Type = type, Title = title, TargetName = targetName, State = TaskState.Pending };
        items.Add(task.Id, task);
        return (task, CancellationToken.None);
    }
    public BackgroundTaskItem? GetTask(string taskId) => items.GetValueOrDefault(taskId);
    public void UpdateProgress(string taskId, int progress, string message, TaskState state = TaskState.Running)
    {
        var task = items[taskId]; task.Progress = progress; task.Message = message; task.State = state;
    }
    public void SetSuccess(string taskId, string message = "已完成") => UpdateProgress(taskId, 100, message, TaskState.Success);
    public void SetFailed(string taskId, string error) => UpdateProgress(taskId, 0, error, TaskState.Failed);
    public bool CancelTask(string taskId, string userId, bool isAdmin)
    {
        Cancellations++;
        UpdateProgress(taskId, 0, "canceled", TaskState.Canceled);
        return true;
    }
    public bool DeleteTask(string taskId, string userId, bool isAdmin) => items.Remove(taskId);
}
