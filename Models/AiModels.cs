using System.Text.Json.Serialization;

namespace MSLX.Plugin.ElementsAI.Models;

public sealed record UserContext(string UserId, bool IsAdmin, string Scope);

public sealed class ChatPreferences
{
    public bool SendOnEnter { get; set; } = true;
}

public class SavedModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..24];
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool? ThinkingEnabled { get; set; }
    public string ThinkingEffort { get; set; } = "medium";
}

public sealed class ModelInput
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool ClearApiKey { get; set; }
    public bool? ThinkingEnabled { get; set; }
    public string ThinkingEffort { get; set; } = "medium";
}

public sealed class ModelOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public bool? HasApiKey { get; set; }
    public bool? ThinkingEnabled { get; set; }
    public string ThinkingEffort { get; set; } = "medium";
}

public sealed class ResolvedModel : SavedModel
{
    public string SelectionId { get; set; } = string.Empty;
    public bool PublicOnly { get; set; }
}

public sealed class AiStatus
{
    public bool Ready { get; set; }
    public bool Admin { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<ModelOption> Models { get; set; } = [];
    public ChatPreferences Preferences { get; set; } = new();
}

public sealed class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public uint? CurrentInstanceId { get; set; }
    public string PermissionMode { get; set; } = "default";
}

public sealed class ApprovalInput
{
    public bool Approved { get; set; }
}

public sealed class QuestionInput
{
    public string Answer { get; set; } = string.Empty;
}

public sealed class DeleteConversationsInput
{
    public List<string> Ids { get; set; } = [];
}

public sealed class FileDiff
{
    public string Path { get; set; } = string.Empty;
    public string Patch { get; set; } = string.Empty;
    public bool Truncated { get; set; }
}

public sealed class ToolApproval
{
    public string Id { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public sealed class ToolQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
}

public sealed class ChatMessage
{
    public string Role { get; set; } = "assistant";
    public string Content { get; set; } = string.Empty;
    public string? Tool { get; set; }
    public bool? Ok { get; set; }
    public bool? Pending { get; set; }
    public FileDiff? Diff { get; set; }
    public ToolApproval? Approval { get; set; }
    public ToolQuestion? Question { get; set; }
    public string? Reasoning { get; set; }
    public bool? ReasoningComplete { get; set; }
    public bool? WorkComplete { get; set; }
}

public class ConversationSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
}

public sealed class ConversationDetail : ConversationSummary
{
    public List<ChatMessage> Messages { get; set; } = [];
}

public sealed class ProviderMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ProviderToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

public sealed class ProviderToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public ProviderFunctionCall Function { get; set; } = new();
}

public sealed class ProviderFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public sealed class ConversationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Owner { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
    public List<ProviderMessage> Context { get; set; } = [];
}

public sealed class UserData
{
    public List<SavedModel> Models { get; set; } = [];
    public ChatPreferences Preferences { get; set; } = new();
    public List<ConversationRecord> Conversations { get; set; } = [];
}

public sealed class ToolExecutionResult
{
    public object? Value { get; set; }
    public FileDiff? Diff { get; set; }
}

public sealed class ProviderCompletion
{
    public string Content { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<ProviderToolCall> ToolCalls { get; set; } = [];
}
