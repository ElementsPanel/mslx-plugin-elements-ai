namespace MSLX.Plugin.ElementsAI.Models;

public sealed record AiTaskProgress(
    string TaskId, string Title, string State, double? Value, string Message,
    string? FileName = null, string? Speed = null, uint? InstanceId = null)
{
    public bool Completed => State is "success" or "failed" or "canceled";
    public bool Success => State == "success";
}

public sealed record TaskWaitResult(string TaskId, bool Completed, bool Success, bool WaitTimedOut, AiTaskProgress Task);
