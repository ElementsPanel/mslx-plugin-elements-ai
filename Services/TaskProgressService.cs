using System.Diagnostics;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.SDK.IServices;
using MSLX.SDK.Models.Files;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed class TaskProgressService(IBackgroundTaskManager tasks)
{
    public AiTaskProgress Read(string taskId, UserContext user)
    {
        if (taskId.Length != 32 || !taskId.All(char.IsAsciiHexDigit))
            throw new ToolException("任务 ID 无效。");
        var task = tasks.GetTask(taskId);
        if (task is null || task.UserId != user.UserId)
            throw new ToolException("任务不存在、已过期或不属于当前账号。");
        var taskState = task.State;
        var state = taskState switch
        {
            TaskState.Pending => "pending", TaskState.Running => "running",
            TaskState.Success => "success", TaskState.Failed => "failed", TaskState.Canceled => "canceled",
            _ => throw new ToolException("任务状态无效。")
        };
        return new(task.Id, task.Title, state,
            taskState == TaskState.Success ? 100 : task.Progress < 0 ? null : Math.Clamp(task.Progress, 0, 100),
            task.Message, task.TargetName, InstanceId: task.InstanceId);
    }

    public (string TaskId, CancellationToken Token) StartDownload(UserContext user, uint instanceId, string fileName)
    {
        var (task, token) = tasks.CreateTask(user.UserId, instanceId, TaskType.Download, "下载 " + fileName, fileName);
        return (task.Id, token);
    }

    public void Update(string taskId, double value, string message) =>
        tasks.UpdateProgress(taskId, double.IsFinite(value) ? (int)Math.Clamp(value, 0, 100) : 0, message);

    public void Complete(string taskId, string message) => tasks.SetSuccess(taskId, message);
    public void Fail(string taskId, string message) => tasks.SetFailed(taskId, message);
    public void Cancel(string taskId, UserContext user) => tasks.CancelTask(taskId, user.UserId, user.IsAdmin);

    public async Task<TaskWaitResult> WaitAsync(
        string taskId, UserContext user, int timeoutSeconds, Func<AiTaskProgress, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        if (timeoutSeconds is < 1 or > 300) throw new ToolException("等待时间必须为 1 到 300 秒。");
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = Read(taskId, user);
            if (onProgress is not null) await onProgress(progress);
            if (progress.Completed) return new(taskId, true, progress.Success, false, progress);
            var remaining = TimeSpan.FromSeconds(timeoutSeconds) - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero) return new(taskId, false, false, true, progress);
            await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
