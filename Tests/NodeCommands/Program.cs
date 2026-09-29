using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("stdout, stderr and nonzero exit code", async () =>
    {
        var result = await NodeCommandService.ExecuteLocalAsync(new()
        {
            Command = OperatingSystem.IsWindows() ? "echo hello& echo problem 1>&2& exit /b 7" : "printf hello; printf problem >&2; exit 7"
        }, default);
        Check(result.ExitCode == 7 && result.Stdout.Trim() == "hello" && result.Stderr.Trim() == "problem", "command output or exit code lost");
        Check(!result.TimedOut && !result.Truncated, "normal command marked incomplete");
    }),
    ("validation prevents starting invalid requests", async () =>
    {
        foreach (var request in new NodeCommandRequest[]
        {
            new() { Command = " " }, new() { Command = "echo\0bad" },
            new() { Command = new string('a', 4097) },
            new() { Command = "echo test", TimeoutSeconds = 0 },
            new() { Command = "echo test", TimeoutSeconds = 121 },
            new() { Command = "echo test", WorkingDirectory = "relative" }
        }) await Throws<ToolException>(() => NodeCommandService.ExecuteLocalAsync(request, default));
    }),
    ("non-admin cannot discover or invoke node tools", async () =>
    {
        var publicTools = MslxToolService.Definitions(false).ToJsonString();
        Check(!publicTools.Contains("execute_node_command") && !publicTools.Contains("list_nodes"), "admin tools exposed");
        Check(MslxToolService.SensitiveTools.Contains("execute_node_command"), "confirmation missing");
        var tools = new MslxToolService(null!, null!, null!, new("user", false, "scope"), new(null, null, null), null!);
        using var arguments = JsonDocument.Parse("{}");
        foreach (var name in new[] { "list_nodes", "execute_node_command" })
            await Throws<ToolException>(() => tools.ExecuteAsync(name, arguments.RootElement, null, default));
    }),
    ("remote routing uses registered address and host credentials", async () =>
    {
        var factory = new FakeClients(async message =>
        {
            Check(message.Headers.GetValues("x-user-token").Single() == "test-token", "authentication missing");
            if (message.Method == HttpMethod.Get)
            {
                Check(message.RequestUri!.ToString() == "http://master.test/api/node/list", "wrong discovery host");
                Check(!message.Headers.Contains("x-node-id"), "discovery inherited a stale node header");
                return Nodes();
            }
            Check(message.RequestUri!.ToString() == "https://worker.test/panel/" + NodeCommandService.Route, "wrong command target");
            Check(message.Headers.GetValues("x-node-id").Single() == "worker-1", "wrong routing ID");
            var body = await message.Content!.ReadFromJsonAsync<NodeCommandRequest>();
            Check(body!.Command == "echo test" && body.TimeoutSeconds == 4 && body.WorkingDirectory == "/tmp", "command payload changed");
            return Packet(new NodeCommandResult { WorkingDirectory = "/tmp", Stdout = "test", ExitCode = 3 });
        });
        var service = new NodeCommandService(factory);
        var host = new MslxHostRequestContext("http://master.test", "test-token", "stale-node");
        var nodes = await service.ListNodesAsync(host, default);
        Check(nodes.Count == 2 && nodes[0].Local && nodes[1].NodeId == "worker-1", "discovery incorrect");
        Check(!JsonSerializer.Serialize(nodes).Contains("secret"), "node secrets exposed to model");
        var result = await service.ExecuteAsync("worker-1", new() { Command = "echo test", TimeoutSeconds = 4, WorkingDirectory = "/tmp" }, host, default);
        Check(result.NodeId == "worker-1" && result.Stdout == "test" && result.ExitCode == 3, "remote result lost");
        Check(factory.Requests == 3, "command retried");
    }),
    ("unknown node cannot execute on local or arbitrary address", async () =>
    {
        var factory = new FakeClients(message =>
        {
            Check(message.Method == HttpMethod.Get, "unknown node received command");
            return Task.FromResult(Nodes());
        });
        await Throws<ToolException>(() => new NodeCommandService(factory).ExecuteAsync("https://unregistered.test", new() { Command = "echo test" }, Host(), default));
        Check(factory.Requests == 1, "unexpected network request");
    }),
    ("remote failures and malformed results are not retried or reported successful", async () =>
    {
        foreach (var status in new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.InternalServerError, HttpStatusCode.OK })
        {
            var factory = new FakeClients(message => Task.FromResult(message.Method == HttpMethod.Get
                ? Nodes()
                : status == HttpStatusCode.OK ? Packet(new { }) : new HttpResponseMessage(status)));
            await Throws<ToolException>(() => new NodeCommandService(factory).ExecuteAsync("worker-1", new() { Command = "echo test" }, Host(), default));
            Check(factory.Requests == 2, "failed command was retried");
        }
    })
};

if (OperatingSystem.IsWindows())
{
    tests.Add(("cmd preserves quoted command text", async () =>
    {
        var result = await NodeCommandService.ExecuteLocalAsync(new() { Command = "echo \"a b\"" }, default);
        Check(result.ExitCode == 0 && result.Stdout.Trim() == "\"a b\"", "cmd quoting changed");
    }));
}
else
{
    tests.Add(("working directory and shell quoting", async () =>
    {
        var result = await NodeCommandService.ExecuteLocalAsync(new() { Command = "pwd; printf '%s' 'a b \"quoted\" $literal'", WorkingDirectory = "/tmp" }, default);
        Check(result.ExitCode == 0 && result.Stdout == "/tmp\na b \"quoted\" $literal", "shell quoting or working directory changed");
    }));
    tests.Add(("large output is drained and bounded", async () =>
    {
        var result = await NodeCommandService.ExecuteLocalAsync(new()
        {
            Command = "head -c 40000 /dev/zero | tr '\\0' x; head -c 40000 /dev/zero | tr '\\0' y >&2"
        }, default);
        Check(result.ExitCode == 0 && result.Truncated && result.Stdout.Length == 16000 && result.Stderr.Length == 16000, "output bound or pipe draining failed");
    }));
    tests.Add(("timeout and cancellation terminate child processes", async () =>
    {
        var directory = Directory.CreateTempSubdirectory("elements-ai-command-test-");
        try
        {
            var result = await NodeCommandService.ExecuteLocalAsync(new()
            {
                Command = "printf started; sleep 3; touch timeout-marker", WorkingDirectory = directory.FullName, TimeoutSeconds = 1
            }, default);
            Check(result.TimedOut && result.Stdout == "started", "timeout did not preserve partial output");
            using var cancellation = new CancellationTokenSource(500);
            await Throws<OperationCanceledException>(() => NodeCommandService.ExecuteLocalAsync(new()
            {
                Command = "sleep 3; touch cancel-marker", WorkingDirectory = directory.FullName
            }, cancellation.Token));
            await Task.Delay(3200);
            Check(!directory.EnumerateFiles().Any(), "child process survived cancellation or timeout");
        }
        finally { directory.Delete(true); }
    }));
}

foreach (var test in tests)
{
    await test.Run();
    Console.WriteLine("PASS " + test.Name);
}
Console.WriteLine($"{tests.Count} tests passed.");

static MslxHostRequestContext Host() => new("http://master.test", "test-token", null);
static HttpResponseMessage Nodes() => Packet(new[] { new { nodeId = "worker-1", nodeName = "Worker", nodeUrl = "https://worker.test/panel", commsKey = "secret" } });
static HttpResponseMessage Packet(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { code = 200, data = value }) };
static void Check(bool value, string message) { if (!value) throw new Exception(message); }
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

sealed class FakeClients(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : IHttpClientFactory
{
    public int Requests { get; private set; }
    public HttpClient CreateClient(string name) => new(new Handler(message =>
    {
        Requests++;
        return send(message);
    }));
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
