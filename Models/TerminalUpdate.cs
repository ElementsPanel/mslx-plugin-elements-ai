namespace MSLX.Plugin.ElementsAI.Models;

public sealed record TerminalReadResult(uint InstanceId, string Content, string Cursor, bool Truncated);

public sealed record TerminalWaitResult(
    uint InstanceId, bool Updated, bool WaitTimedOut, string Cursor,
    string Content, string ContentMode, bool Truncated);

public sealed record TerminalWaitHeartbeat;
