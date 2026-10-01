using System.Net;
using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;

static class ToolCallHistoryTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("five missing resource receipts are repaired before later user turns without inventing success", () =>
        {
            var context = new List<ProviderMessage>
            {
                User("查询资源"), Assistant(Enumerable.Range(11, 5).Select(i => Call("list_resource_versions:" + i)).ToArray()),
                new() { Role = "assistant", Content = "AI 请求失败，请稍后重试。" }, User("继续"),
                new() { Role = "assistant", Content = "HTTP 400" }, User("继续")
            };
            var repaired = ProviderHistory.Repair(context);
            AssertProtocol(repaired);
            Check(repaired.Count == context.Count + 5 && repaired.Count(m => m.Role == "user") == 3, "history discarded");
            Check(repaired.Where(m => m.Role == "tool").All(m => m.Content!.Contains("result_unavailable") && m.Content.Contains("\"success\":false")), "missing receipt invented success");
            Check(JsonSerializer.Serialize(repaired) == JsonSerializer.Serialize(ProviderHistory.Repair(repaired)), "repair is not idempotent");
            return Task.CompletedTask;
        });
        yield return ("history repair preserves real results and pairs only adjacent tool replies", () =>
        {
            var known = new ProviderMessage { Role = "tool", ToolCallId = "one", Content = "{\"created\":true,\"taskId\":\"task\"}" };
            var context = new List<ProviderMessage>
            {
                User("work"), Assistant(Call("one", "create_instance"), Call("two")),
                new() { Role = "tool", ToolCallId = "two", Content = "second result" }, known,
                new() { Role = "tool", ToolCallId = "one", Content = "duplicate result" },
                User("next"), Assistant(Call("one", "create_instance")), User("continue"),
                new() { Role = "tool", ToolCallId = "orphan", Content = "orphan result" }
            };
            var repaired = ProviderHistory.Repair(context);
            AssertProtocol(repaired);
            Check(ReferenceEquals(repaired[2], known) && repaired[3].Content == "second result", "completed receipts changed");
            Check(repaired[6].Content!.Contains("result_unavailable") && repaired[6].Content!.Contains("\"retryable\":false"), "earlier reused ID mistaken for later result");
            Check(!repaired.Any(m => m.Content == "duplicate result" || m.Content == "orphan result"), "invalid replies retained");
            return Task.CompletedTask;
        });
        yield return ("malformed historical call IDs cannot poison the next provider request", () =>
        {
            var calls = new[] { Call(""), Call("valid"), Call("valid"), Call("bad-name", "") };
            var repaired = ProviderHistory.Repair([User("work"), Assistant(calls), User("continue")]);
            AssertProtocol(repaired);
            Check(repaired[1].ToolCalls!.Count == 1 && repaired[2].ToolCallId == "valid", "malformed calls not removed");
            return Task.CompletedTask;
        });
        yield return ("invalid new batches are rejected before recording or executing any call", () =>
        {
            foreach (var calls in new[] { new[] { Call("same"), Call("same") }, new[] { Call("good"), Call("") }, new[] { Call("seen") } })
            {
                var context = new List<ProviderMessage> { User("work") };
                var seen = new HashSet<string> { "seen" };
                try { _ = new ToolCallBatch(context, new() { ToolCalls = calls.ToList() }, seen); }
                catch (ToolException)
                {
                    Check(context.Count == 1 && seen.Count == 1, "invalid batch partially committed");
                    continue;
                }
                throw new Exception("invalid batch accepted");
            }
            return Task.CompletedTask;
        });
        yield return ("tool network, argument and lazy result errors allow remaining calls and a corrected retry", async () =>
        {
            var context = new List<ProviderMessage> { User("work") };
            var seen = new HashSet<string>();
            var calls = Enumerable.Range(1, 5).Select(i => Call("call" + i)).ToList();
            calls[1].Function.Arguments = "{broken";
            var batch = new ToolCallBatch(context, new() { ToolCalls = calls }, seen);
            var invoked = new List<int>();
            for (var i = 0; i < calls.Count; i++)
            {
                var index = i;
                var outcome = await batch.ExecuteAsync(i, _ =>
                {
                    invoked.Add(index);
                    if (index == 0) throw new HttpRequestException("private-key-in-url", null, HttpStatusCode.BadGateway);
                    return Task.FromResult(new ToolExecutionResult { Value = index == 2 ? new { items = BrokenItems() } : (object)new { found = true } });
                }, default);
                Check(outcome.Result.Ok == (i >= 3), "tool failure escaped or was reported successful");
                Check(!outcome.Content.Contains("private-key"), "raw SDK exception leaked");
                AssertProtocol(context);
            }
            Check(invoked.SequenceEqual(new[] { 0, 2, 3, 4 }), "bad args executed or later calls stopped");
            var retry = new ToolCallBatch(context, new() { ToolCalls = [Call("retry")] }, seen);
            var recovered = await retry.ExecuteAsync(0, _ => Task.FromResult(new ToolExecutionResult { Value = new { found = true } }), default);
            Check(recovered.Result.Ok, "corrected tool retry failed");
            _ = new ToolCallBatch(context, new() { Content = "完成" }, seen);
            AssertProtocol(context);
        });
        yield return ("cancellation preserves completed receipts and distinguishes unknown from unexecuted calls", async () =>
        {
            var context = new List<ProviderMessage> { User("work") };
            var batch = new ToolCallBatch(context, new() { ToolCalls = [Call("one"), Call("two", "create_instance"), Call("three")] }, new HashSet<string>());
            await batch.ExecuteAsync(0, _ => Task.FromResult(new ToolExecutionResult { Value = new { success = true } }), default);
            using var cancel = new CancellationTokenSource();
            await Throws<OperationCanceledException>(() => batch.ExecuteAsync(1, _ =>
            {
                cancel.Cancel();
                throw new OperationCanceledException(cancel.Token);
            }, cancel.Token));
            await Throws<OperationCanceledException>(() => batch.ExecuteAsync(2, _ => throw new Exception("must not execute"), cancel.Token));
            AssertProtocol(context);
            Check(context[2].Content == "{\"success\":true}", "completed result lost");
            Check(context[3].Content!.Contains("result_unavailable") && context[3].Content!.Contains("\"retryable\":false"), "unknown write falsely marked unexecuted/retryable");
            Check(context[4].Content!.Contains("not_executed"), "queued call falsely marked executed");
        });
        yield return ("client transport failures escape tool recovery while every call still has a receipt", async () =>
        {
            var context = new List<ProviderMessage>();
            var batch = new ToolCallBatch(context, new() { ToolCalls = [Call("one"), Call("two")] }, new HashSet<string>());
            var completed = await batch.ExecuteAsync(0, _ => Task.FromResult(new ToolExecutionResult { Value = new { saved = true } }), default);
            Check(context[1].Content == completed.Content, "receipt not committed before browser notification");
            await Throws<ChatStreamException>(() => batch.ExecuteAsync(1, _ => throw new ChatStreamException(new IOException("closed")), default));
            AssertProtocol(context);
            Check(context[1].Content!.Contains("\"saved\":true"), "transport failure erased completed result");
        });
        yield return ("tool timeouts are recoverable but uncertain writes and approval denials are not blindly retried", async () =>
        {
            var context = new List<ProviderMessage>();
            var batch = new ToolCallBatch(context, new() { ToolCalls = [Call("timeout"), Call("write", "create_instance"), Call("denied", "edit_file")] }, new HashSet<string>());
            var timeout = await batch.ExecuteAsync(0, _ => throw new TaskCanceledException("timeout"), default);
            Check(!timeout.Result.Ok && timeout.Content.Contains("\"retryable\":true"), "tool timeout ended turn");
            var write = await batch.ExecuteAsync(1, _ => throw new IOException("private-key"), default);
            Check(!write.Result.Ok && write.Content.Contains("\"retryable\":false") && !write.Content.Contains("private-key"), "uncertain write retried");
            var denied = await batch.ExecuteAsync(2, _ => Task.FromResult(new ToolExecutionResult { Ok = false, Value = new { status = "denied", retryable = false } }), default);
            Check(!denied.Result.Ok && denied.Content.Contains("denied") && denied.Content.Contains("\"retryable\":false"), "approval denial changed");
            AssertProtocol(context);
        });
    }

    private static IEnumerable<int> BrokenItems() { yield return 1; throw new InvalidOperationException("private-key"); }
    private static ProviderMessage User(string content) => new() { Role = "user", Content = content };
    private static ProviderMessage Assistant(params ProviderToolCall[] calls) => new() { Role = "assistant", ToolCalls = calls.ToList() };
    private static ProviderToolCall Call(string id, string tool = "list_resource_versions") => new() { Id = id, Function = new() { Name = tool, Arguments = "{}" } };

    // Independent protocol check: each assistant batch is followed by exactly one reply per call,
    // before any user/assistant message. Standalone tool messages are rejected.
    private static void AssertProtocol(IReadOnlyList<ProviderMessage> messages)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            Check(message.Role != "tool", "orphan tool reply");
            if (message.ToolCalls is not { Count: > 0 } calls) continue;
            var expected = calls.Select(c => c.Id).ToHashSet();
            Check(expected.Count == calls.Count && expected.All(id => !string.IsNullOrWhiteSpace(id)), "invalid call IDs");
            for (var count = 0; count < calls.Count; count++)
            {
                Check(++i < messages.Count && messages[i].Role == "tool", "missing tool reply before next turn");
                Check(expected.Remove(messages[i].ToolCallId!) && !string.IsNullOrEmpty(messages[i].Content), "duplicate/mismatched/empty tool reply");
            }
            Check(expected.Count == 0, "unmatched tool call IDs");
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
