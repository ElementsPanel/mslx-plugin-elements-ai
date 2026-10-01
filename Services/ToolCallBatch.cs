using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed record ToolCallOutcome(ToolExecutionResult Result, string Content);

// Transport failures must escape the tool-error boundary and end the chat turn.
public sealed class ChatStreamException(Exception inner) : Exception("聊天连接已中断。", inner);

public sealed class ToolCallBatch
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private readonly IReadOnlyList<ProviderToolCall> _calls;
    private readonly List<ProviderMessage> _replies;

    public ToolCallBatch(List<ProviderMessage> context, ProviderCompletion completion, ISet<string> seenCallIds)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var call in completion.ToolCalls)
        {
            if (!Valid(call) || !ids.Add(call.Id) || seenCallIds.Contains(call.Id))
                throw new ToolException("模型返回了重复或无效的工具调用。");
        }
        foreach (var id in ids) seenCallIds.Add(id);
        _calls = completion.ToolCalls;
        _replies = _calls.Select(call => new ProviderMessage
        {
            Role = "tool", ToolCallId = call.Id,
            Content = Serialize(new { success = false, status = "not_executed", error = "本轮在执行此工具前已结束，未执行该调用。" })
        }).ToList();
        // Commit the entire protocol block before emitting events or running any tool.
        // Each placeholder is replaced in place as its result becomes known.
        context.Add(new ProviderMessage
        {
            Role = "assistant", Content = completion.Content,
            ToolCalls = _calls.Count > 0 ? completion.ToolCalls : null
        });
        context.AddRange(_replies);
    }

    public async Task<ToolCallOutcome> ExecuteAsync(int index,
        Func<JsonElement, Task<ToolExecutionResult>> execute, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var call = _calls[index];
        ToolExecutionResult result;
        string content;
        var invoked = false;
        try
        {
            using var arguments = ParseArguments(call.Function.Arguments);
            // A cancellation/disconnect can occur after a side effect, before its receipt arrives.
            _replies[index].Content = MissingResult(call.Function.Name);
            invoked = true;
            result = await execute(arguments.RootElement);
            // Serialization may enumerate lazy SDK results and throw too.
            content = Serialize(result.Value ?? new { });
        }
        catch (Exception error) when (error is not (ChatStreamException or OutOfMemoryException) && !cancellationToken.IsCancellationRequested)
        {
            var retryable = !invoked || ReadOnly(call.Function.Name);
            var detail = error switch
            {
                ToolException or AiValidationException => error.Message,
                HttpRequestException http when http.StatusCode.HasValue => $"工具请求失败（HTTP {(int)http.StatusCode.Value}）。",
                HttpRequestException or IOException => "工具连接失败或响应中断。",
                OperationCanceledException or TimeoutException => "工具请求超时。",
                _ => "工具执行或读取结果时发生异常。"
            };
            result = new ToolExecutionResult
            {
                Ok = false,
                Value = new
                {
                    success = false, status = invoked ? "tool_error" : "not_executed", error = detail,
                    retryable,
                    nextAction = retryable
                        ? "根据错误修正参数或重试此工具，继续处理用户任务；若持续失败，说明原因或选择其他可用查询方式。"
                        : "先核实操作状态；不要盲目重复写入或绕过用户拒绝、账号权限。"
                }
            };
            content = Serialize(result.Value!);
        }
        // Save before sending to the browser so a broken SSE connection cannot lose a result.
        _replies[index].Content = content;
        return new ToolCallOutcome(result, content);
    }

    public static bool Valid(ProviderToolCall? call) => call is not null
        && !string.IsNullOrWhiteSpace(call.Id) && call.Type == "function"
        && call.Function is not null && !string.IsNullOrWhiteSpace(call.Function.Name);

    public static string MissingResult(string tool) => Serialize(new
    {
        success = false, status = "result_unavailable", error = "上次调用没有保存完整回执，无法确认执行结果。",
        retryable = ReadOnly(tool),
        nextAction = ReadOnly(tool) ? "可重新执行此查询以获取结果。" : "先查询当前状态，勿盲目重复有副作用的操作。"
    });

    private static bool ReadOnly(string tool) => tool is
        "list_instances" or "get_instance" or "read_terminal" or "wait_for_terminal_update"
        or "list_nodes" or "list_files" or "read_file" or "search_resources" or "list_resource_versions"
        or "list_msl_cores" or "list_msl_core_versions" or "list_msl_java_versions" or "wait_for_task"
        or "list_mslfrp_nodes" or "list_mslfrp_tunnels";

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static JsonDocument ParseArguments(string? value)
    {
        try
        {
            var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
            if (document.RootElement.ValueKind == JsonValueKind.Object) return document;
            document.Dispose();
            throw new ToolException("工具参数必须是 JSON 对象。");
        }
        catch (JsonException) { throw new ToolException("模型返回了无效的工具参数。"); }
    }
}
