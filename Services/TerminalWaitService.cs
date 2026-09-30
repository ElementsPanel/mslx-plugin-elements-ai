using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.SDK.IServices;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed class TerminalWaitService(IInstanceConsoleService console, Action<uint> requireAccess)
{
    private const int SnapshotChars = 32000;
    private readonly Dictionary<uint, Snapshot> _lastRead = new();

    public TerminalReadResult Read(uint instanceId, int lines = 100, int maxChars = 16000)
    {
        ValidateLimits(lines, maxChars);
        var snapshot = Capture(instanceId);
        _lastRead[instanceId] = snapshot;
        var (content, truncated) = Limit(snapshot.Content, lines, maxChars);
        return new(instanceId, content, snapshot.Cursor, truncated || snapshot.Truncated);
    }

    public async Task<TerminalWaitResult> WaitAsync(
        uint instanceId, string? cursor = null, int timeoutSeconds = 60, int lines = 100, int maxChars = 16000,
        Func<Task>? heartbeat = null, CancellationToken cancellationToken = default)
    {
        ValidateLimits(lines, maxChars);
        if (timeoutSeconds is < 1 or > 300) throw new ToolException("等待时间必须为 1 到 300 秒。");
        if (cursor is not null && (cursor.Length != 64 || !cursor.All(char.IsAsciiHexDigit)))
            throw new ToolException("终端游标无效，请使用 read_terminal 或上次等待返回的 cursor。");
        cancellationToken.ThrowIfCancellationRequested();
        var elapsed = Stopwatch.StartNew();
        var current = Capture(instanceId);
        _lastRead.TryGetValue(instanceId, out var previous);
        var baselineCursor = cursor?.ToLowerInvariant() ?? previous?.Cursor ?? current.Cursor;
        var baseline = previous?.Cursor == baselineCursor ? previous : current.Cursor == baselineCursor ? current : null;
        var nextHeartbeat = TimeSpan.FromSeconds(15);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current.Cursor != baselineCursor)
            {
                _lastRead[instanceId] = current;
                var (text, mode) = ChangedContent(baseline, current);
                var (content, truncated) = Limit(text, lines, maxChars);
                return new(instanceId, true, false, current.Cursor, content, mode,
                    truncated || (mode == "snapshot" && current.Truncated));
            }
            var remaining = TimeSpan.FromSeconds(timeoutSeconds) - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                _lastRead[instanceId] = current;
                return new(instanceId, false, true, current.Cursor, string.Empty, "none", false);
            }
            if (heartbeat is not null && elapsed.Elapsed >= nextHeartbeat)
            {
                await heartbeat();
                nextHeartbeat = elapsed.Elapsed + TimeSpan.FromSeconds(15);
            }
            await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1), cancellationToken);
            current = Capture(instanceId);
        }
    }

    private Snapshot Capture(uint instanceId)
    {
        requireAccess(instanceId);
        var pty = console.IsServerPtyMode(instanceId);
        var logs = pty ? console.GetPtyHistory(instanceId) : console.GetLogs(instanceId);
        var text = string.Join("\n", logs.TakeLast(500).Select(line => Regex.Replace(line,
            @"\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\x07]*(?:\x07|\x1B\\))", string.Empty)));
        var truncated = logs.Count > 500 || text.Length > SnapshotChars;
        if (text.Length > SnapshotChars) text = text[^SnapshotChars..];
        requireAccess(instanceId);
        var cursor = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((pty ? "pty:" : "log:") + text))).ToLowerInvariant();
        return new(text, cursor, pty, truncated);
    }

    private static (string Content, string Mode) ChangedContent(Snapshot? previous, Snapshot current)
    {
        var currentContent = current.Content;
        if (previous is null || previous.Pty != current.Pty) return (currentContent, "snapshot");
        if (currentContent.StartsWith(previous.Content, StringComparison.Ordinal))
            return (currentContent[previous.Content.Length..], "delta");
        // Find suffix/prefix overlap in linear time when the host's terminal buffer rolls over.
        var prefix = new int[currentContent.Length];
        for (var i = 1; i < currentContent.Length; i++)
        {
            var matched = prefix[i - 1];
            while (matched > 0 && currentContent[i] != currentContent[matched]) matched = prefix[matched - 1];
            if (currentContent[i] == currentContent[matched]) matched++;
            prefix[i] = matched;
        }
        var overlap = 0;
        foreach (var character in previous.Content)
        {
            while (overlap > 0 && (overlap == currentContent.Length || currentContent[overlap] != character))
                overlap = prefix[overlap - 1];
            if (overlap < currentContent.Length && currentContent[overlap] == character) overlap++;
        }
        return overlap > 0 && overlap < currentContent.Length
            ? (currentContent[overlap..], "delta") : (currentContent, "snapshot");
    }

    private static (string Content, bool Truncated) Limit(string text, int lines, int maxChars)
    {
        var content = string.Join("\n", text.Split('\n').TakeLast(lines));
        if (content.Length > maxChars) content = content[^maxChars..];
        return (content, content.Length < text.Length);
    }

    private static void ValidateLimits(int lines, int maxChars)
    {
        if (lines is < 1 or > 500 || maxChars is < 100 or > SnapshotChars)
            throw new ToolException("终端输出限制无效。");
    }

    private sealed record Snapshot(string Content, string Cursor, bool Pty, bool Truncated);
}
