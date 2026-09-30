using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;
using MSLX.SDK.IServices;

static class TerminalWaitTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("terminal reads return stable cursors with bounded sanitized output", () =>
        {
            var console = new TerminalConsole { Logs = Enumerable.Range(0, 150).Select(i => "line" + i).ToList() };
            console.Logs.Add("\u001b[32mready\u001b[0m");
            var service = new TerminalWaitService(console, _ => { });
            var result = service.Read(1, 2, 100);
            Check(result.Content == "line149\nready" && result.Truncated && result.Cursor.Length == 64, "read bounds or cursor incorrect");
            Check(service.Read(1, 50, 1000).Cursor == result.Cursor, "display limits changed the cursor");
            return Task.CompletedTask;
        });
        yield return ("terminal wait sees output arriving between tool calls and returns only new text", async () =>
        {
            var console = new TerminalConsole { Logs = ["starting"] };
            var service = new TerminalWaitService(console, _ => { });
            var first = service.Read(1);
            console.Logs.Add("ready");
            var result = await service.WaitAsync(1, first.Cursor, 1);
            Check(result.Updated && !result.WaitTimedOut && result.Content == "\nready" && result.ContentMode == "delta", "existing baseline missed new output");
            Check(result.Cursor != first.Cursor, "updated output kept old cursor");
            console.Logs.Add("players joined");
            var next = await service.WaitAsync(1, timeoutSeconds: 1);
            Check(next.Content == "\nplayers joined", "implicit last-read cursor repeated logs");
        });
        yield return ("terminal wait polls internally until content changes", async () =>
        {
            var console = new TerminalConsole { Logs = ["starting"] };
            var service = new TerminalWaitService(console, _ => { });
            service.Read(1);
            console.OnRead = () => { if (console.Reads == 3) console.Logs.Add("ready"); };
            var result = await service.WaitAsync(1, timeoutSeconds: 2);
            Check(result.Updated && result.Content == "\nready" && console.Reads == 3, "wait did not observe later output");
        });
        yield return ("terminal wait timeout returns no repeated logs", async () =>
        {
            var console = new TerminalConsole { Logs = ["unchanged"] };
            var service = new TerminalWaitService(console, _ => { });
            var first = service.Read(1);
            var result = await service.WaitAsync(1, first.Cursor, 1);
            Check(!result.Updated && result.WaitTimedOut && result.Content == "" && result.ContentMode == "none" && result.Cursor == first.Cursor,
                "timeout repeated logs or reported an update");
        });
        yield return ("terminal wait handles buffer rollover, clearing and PTY changes", async () =>
        {
            var console = new TerminalConsole { Logs = ["first", "second"] };
            var service = new TerminalWaitService(console, _ => { });
            service.Read(1);
            console.Logs = ["second", "third"];
            var rollover = await service.WaitAsync(1, timeoutSeconds: 1);
            Check(rollover.ContentMode == "delta" && rollover.Content == "\nthird", "rollover repeated old lines");
            console.Logs = [];
            var cleared = await service.WaitAsync(1, timeoutSeconds: 1);
            Check(cleared.Updated && cleared.ContentMode == "snapshot" && cleared.Content == "", "clear not detected");
            console.Pty = true;
            console.PtyLogs = ["\u001b[31mpty output\u001b[0m"];
            var pty = await service.WaitAsync(1, timeoutSeconds: 1);
            Check(pty.ContentMode == "snapshot" && pty.Content == "pty output", "PTY stream not selected or sanitized");
        });
        yield return ("terminal wait returns a bounded snapshot when the baseline is unavailable", async () =>
        {
            var console = new TerminalConsole { Logs = [new string('x', 40000)] };
            var service = new TerminalWaitService(console, _ => { });
            var result = await service.WaitAsync(1, new string('0', 64), 1, maxChars: 100);
            Check(result.Updated && result.ContentMode == "snapshot" && result.Truncated && result.Content.Length == 100, "fallback output is not bounded");
        });
        yield return ("terminal waits validate parameters and honor permission changes and cancellation", async () =>
        {
            var console = new TerminalConsole { Logs = ["unchanged"] };
            var allowed = true;
            var service = new TerminalWaitService(console, _ => { if (!allowed) throw new ToolException("denied"); });
            foreach (var cursor in new[] { "", "bad", new string('x', 64) })
                await Throws<ToolException>(() => service.WaitAsync(1, cursor, 1));
            foreach (var timeout in new[] { 0, 301 })
                await Throws<ToolException>(() => service.WaitAsync(1, timeoutSeconds: timeout));
            await Throws<ToolException>(() => service.WaitAsync(1, lines: 501));
            Check(console.Reads == 0, "invalid requests read terminal data");
            service.Read(1);
            using var cancel = new CancellationTokenSource(30);
            await Throws<OperationCanceledException>(() => service.WaitAsync(1, cancellationToken: cancel.Token));
            console.OnRead = () => allowed = false;
            await Throws<ToolException>(() => service.WaitAsync(1, timeoutSeconds: 1));
            // All mutation methods of the fake throw, so a successful wait cannot send or stop anything.
        });
        yield return ("MSL queries carry their source prefix and terminal waiting is read-only", () =>
        {
            var definitions = MslxToolService.Definitions(false);
            foreach (var name in new[] { "list_msl_cores", "list_msl_core_versions", "list_msl_java_versions" })
            {
                var function = definitions.First(item => item!["function"]!["name"]!.GetValue<string>() == name)!["function"]!;
                Check(function["description"]!.GetValue<string>().StartsWith("MSL镜像源："), "MSL source prefix missing");
            }
            Check(definitions.Any(item => item!["function"]!["name"]!.GetValue<string>() == "wait_for_terminal_update"), "wait tool unavailable to ordinary users");
            Check(!MslxToolService.SensitiveTools.Contains("wait_for_terminal_update"), "read-only wait requires mutation approval");
            return Task.CompletedTask;
        });
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }

    private sealed class TerminalConsole : IInstanceConsoleService
    {
        public List<string> Logs { get; set; } = [];
        public List<string> PtyLogs { get; set; } = [];
        public bool Pty { get; set; }
        public int Reads { get; private set; }
        public Action? OnRead { get; set; }
        public bool IsServerPtyMode(uint instanceId) => Pty;
        public List<string> GetLogs(uint instanceId) { Reads++; OnRead?.Invoke(); return [.. Logs]; }
        public List<string> GetPtyHistory(uint instanceId) { Reads++; OnRead?.Invoke(); return [.. PtyLogs]; }
        public bool SendCommand(uint instanceId, string command, bool repeatCommandToLog = false) => throw new Exception("Unexpected mutation");
        public bool SendPtyInput(uint instanceId, string data) => throw new Exception("Unexpected mutation");
        public bool SendPtyInput(uint instanceId, byte[] data) => throw new Exception("Unexpected mutation");
        public bool ResizePty(uint instanceId, int cols, int rows) => throw new Exception("Unexpected mutation");
    }
}
