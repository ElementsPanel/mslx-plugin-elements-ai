using System.Text.Json.Serialization;

namespace MSLX.Plugin.ElementsAI.Models;

public sealed class NodeCommandRequest
{
    public string Command { get; set; } = string.Empty;
    public string? WorkingDirectory { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class NodeCommandResult
{
    public string NodeId { get; set; } = "local";
    [JsonRequired]
    public string WorkingDirectory { get; set; } = string.Empty;
    [JsonRequired]
    public int ExitCode { get; set; }
    [JsonRequired]
    public string Stdout { get; set; } = string.Empty;
    [JsonRequired]
    public string Stderr { get; set; } = string.Empty;
    [JsonRequired]
    public bool TimedOut { get; set; }
    [JsonRequired]
    public bool Truncated { get; set; }
}

public sealed record NodeOption(string NodeId, string NodeName, bool Local);
