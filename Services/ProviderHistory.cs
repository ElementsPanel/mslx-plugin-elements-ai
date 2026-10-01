using MSLX.Plugin.ElementsAI.Models;

namespace MSLX.Plugin.ElementsAI.Services;

public static class ProviderHistory
{
    // Repair records saved by older versions without discarding user turns or known results.
    // A missing result is explicitly unknown, never invented as a success or re-executed here.
    public static List<ProviderMessage> Repair(IReadOnlyList<ProviderMessage> messages)
    {
        var repaired = new List<ProviderMessage>();
        for (var index = 0; index < messages.Count; index++)
        {
            var message = messages[index];
            if (message.Role == "tool") continue; // orphan/duplicate responses are invalid protocol messages
            if (message.Role != "assistant" || message.ToolCalls is not { Count: > 0 })
            {
                repaired.Add(message);
                continue;
            }
            var calls = message.ToolCalls.Where(ToolCallBatch.Valid).DistinctBy(call => call.Id, StringComparer.Ordinal).ToList();
            var replies = new Dictionary<string, ProviderMessage>(StringComparer.Ordinal);
            while (index + 1 < messages.Count && messages[index + 1].Role == "tool")
            {
                var reply = messages[++index];
                if (!string.IsNullOrWhiteSpace(reply.ToolCallId)) replies.TryAdd(reply.ToolCallId, reply);
            }
            if (calls.Count == 0)
            {
                if (!string.IsNullOrEmpty(message.Content))
                    repaired.Add(new ProviderMessage { Role = "assistant", Content = message.Content });
                continue;
            }
            repaired.Add(new ProviderMessage { Role = "assistant", Content = message.Content, ToolCalls = calls });
            foreach (var call in calls)
            {
                repaired.Add(replies.TryGetValue(call.Id, out var reply) && !string.IsNullOrEmpty(reply.Content)
                    ? reply
                    : new ProviderMessage { Role = "tool", ToolCallId = call.Id, Content = ToolCallBatch.MissingResult(call.Function.Name) });
            }
        }
        return repaired;
    }
}
