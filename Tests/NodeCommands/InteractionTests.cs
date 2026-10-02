using System.Reflection;
using Microsoft.AspNetCore.Http;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;

static class InteractionTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("MSLFRP interactions validate owner and choice without persisting credentials", async () =>
        {
            var chat = new AiChatService(null!, null!, null!, null!, null!, null!, null!, null!);
            var user = new UserContext("alice", true, "scope");
            foreach (var kind in new[] { "mslfrp_login", "mslfrp_node" })
            {
                var message = new ChatMessage(); var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream();
                var emitterType = typeof(AiChatService).GetNestedType("SseEmitter", BindingFlags.NonPublic)!;
                var emitter = Activator.CreateInstance(emitterType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, [context.Response], null)!;
                var task = (Task<string>)typeof(AiChatService).GetMethod("WaitForChoiceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(chat, [kind, "请选择", new List<string> { "#7", "#9" }, message, 0, user, emitter, CancellationToken.None])!;
                var id = message.Question!.Id;
                Check(!chat.RespondToQuestion(user with { UserId = "bob" }, id, "#7", "secret"), "wrong user responded");
                await Throws<ToolException>(() => Task.FromResult(chat.RespondToQuestion(user, id, "bad")));
                Check(!task.IsCompleted, "invalid answer consumed wait");
                Check(chat.RespondToQuestion(user, id, "#7", kind == "mslfrp_login" ? "secret" : null), "valid answer rejected");
                Check(await task == (kind == "mslfrp_login" ? "secret" : "#7"), "answer lost");
                Check(!System.Text.Json.JsonSerializer.Serialize(message).Contains("secret"), "credential persisted");
                Check(!System.Text.Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()).Contains("secret"), "credential streamed");
                Check(chat.GetInteractionStatus(user).QuestionIds.Length == 0, "finished wait remained active");
            }
        });
        yield return ("waiting for an answer keeps the connection alive without completing or auto-approving", async () =>
        {
            var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ticks = 0;
            var heartbeatReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var wait = InteractionWait.WaitAsync(answer.Task, () =>
            {
                if (++ticks >= 3) heartbeatReached.TrySetResult();
                return Task.CompletedTask;
            }, default, TimeSpan.FromMilliseconds(5));
            await heartbeatReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(!wait.IsCompleted && !answer.Task.IsCompleted, "waiting expired or implicitly approved");
            answer.SetResult(true);
            Check(await wait, "answer lost after idle heartbeats");
            var finalTicks = ticks;
            await Task.Delay(20);
            Check(ticks == finalTicks, "heartbeat survived completion");
        });
        yield return ("interaction waiting still stops on explicit cancellation and broken streams", async () =>
        {
            var answer = new TaskCompletionSource<bool>();
            using var cancel = new CancellationTokenSource();
            var wait = InteractionWait.WaitAsync(answer.Task, () => Task.CompletedTask, cancel.Token, TimeSpan.FromMilliseconds(5));
            cancel.Cancel();
            await Throws<OperationCanceledException>(() => wait);
            Check(!answer.Task.IsCompleted, "cancellation granted approval");
            await Throws<ChatStreamException>(() => InteractionWait.WaitAsync(answer.Task,
                () => throw new ChatStreamException(new IOException("closed")), default, TimeSpan.FromMilliseconds(5)));
        });
        yield return ("permission scope tracks identity and grants, independent of instance inventory", () =>
        {
            var first = UserContext.AuthorizationScope("alice", "admin", ["node_1", "instance_2"]);
            Check(first == UserContext.AuthorizationScope("alice", "ADMIN", ["instance_2", "node_1", "node_1"]), "equivalent permissions changed scope");
            Check(first != UserContext.AuthorizationScope("bob", "admin", ["node_1", "instance_2"]), "identity ignored");
            Check(first != UserContext.AuthorizationScope("alice", "user", ["node_1", "instance_2"]), "role ignored");
            Check(first != UserContext.AuthorizationScope("alice", "admin", ["node_1"]), "permission change ignored");
            return Task.CompletedTask;
        });
        yield return ("pending approvals remain valid for their owner and scope and are consumed only once", async () =>
        {
            var chat = new AiChatService(null!, null!, null!, null!, null!, null!, null!, null!);
            var user = new UserContext("alice", true, "stable-permissions");
            var message = new ChatMessage();
            var request = StartApproval(chat, user, message, default);
            var id = message.Approval!.Id;
            Check(chat.GetInteractionStatus(user).ApprovalIds.SequenceEqual(new[] { id }), "live approval missing");
            Check(chat.GetInteractionStatus(user with { UserId = "bob" }).ApprovalIds.Length == 0, "approval exposed to another user");
            Check(!chat.RespondToApproval(user with { UserId = "bob" }, id, true), "different user approved");
            Check(!chat.RespondToApproval(user with { Scope = "changed" }, id, true), "changed permissions approved");
            Check(!request.IsCompleted && message.Approval?.Id == id, "invalid answer consumed pending approval");
            Check(chat.RespondToApproval(user, id, true), "legitimate approval rejected");
            Check(await request, "approved result lost");
            Check(!chat.RespondToApproval(user, id, true), "approval accepted twice");
            Check(chat.GetInteractionStatus(user).ApprovalIds.Length == 0 && message.Approval is null, "finished approval remained active");
        });
        yield return ("canceled confirmations cannot be approved by a stale button", async () =>
        {
            var chat = new AiChatService(null!, null!, null!, null!, null!, null!, null!, null!);
            var user = new UserContext("alice", true, "scope");
            using var cancel = new CancellationTokenSource();
            var message = new ChatMessage();
            var request = StartApproval(chat, user, message, cancel.Token);
            var id = message.Approval!.Id;
            cancel.Cancel();
            Check(!chat.RespondToApproval(user, id, true), "canceled task reported a successful approval");
            await Throws<OperationCanceledException>(() => request);
            Check(chat.GetInteractionStatus(user).ApprovalIds.Length == 0 && message.Approval is null, "canceled approval retained");
        });
    }

    private static Task<bool> StartApproval(AiChatService chat, UserContext user, ChatMessage message, CancellationToken cancellationToken)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var emitterType = typeof(AiChatService).GetNestedType("SseEmitter", BindingFlags.NonPublic)!;
        var emitter = Activator.CreateInstance(emitterType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, [context.Response], null)!;
        return (Task<bool>)typeof(AiChatService).GetMethod("RequestApprovalAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(chat, ["{}", message, 0, user, emitter, cancellationToken])!;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
