using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;

static class MslFrpTests
{
    private static readonly MslxHostRequestContext Host = new("http://panel.test", "panel-secret", "stale-node", "msl-secret");
    private static readonly MslFrpCreateRequest Input = new() { NodeId = 7, Name = "Minecraft", LocalPort = 25565, RemotePort = 30001 };

    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("MSLFRP missing login waits and resumes without exposing credentials", async () =>
        {
            var scenario = new Scenario(); using var service = scenario.Service();
            var login = new TaskCompletionSource<string>(); var prompted = 0;
            var host = Host with { MslFrpToken = null, LoginMslFrp = ct => { prompted++; return login.Task.WaitAsync(ct); } };
            var task = service.ListNodesAsync(host, default);
            Check(prompted == 1 && scenario.Requests == 0 && !task.IsCompleted, "missing login did not wait");
            login.SetResult("msl-secret");
            var receipt = JsonSerializer.Serialize(await task);
            Check(scenario.Requests == 2 && !receipt.Contains("secret"), "login did not resume safely");
        });
        yield return ("MSLFRP expired login resumes on HTTP and provider 401, never forbidden", async () =>
        {
            foreach (var mode in new[] { "http", "provider", "forbidden" })
            {
                var calls = 0; var prompts = 0;
                using var service = new MslFrpService(new HttpClient(new Handler(req =>
                {
                    calls++;
                    if (calls == 1) return Task.FromResult(mode == "provider" ? Packet(null, 401) : new HttpResponseMessage(mode == "http" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden));
                    Check(req.Headers.Authorization!.Parameter == "renewed", "fresh login not used");
                    return Task.FromResult(Packet(System.Array.Empty<object>()));
                })));
                var host = Host with { LoginMslFrp = _ => { prompts++; return Task.FromResult("renewed"); } };
                if (mode == "forbidden") { await Fails(() => service.ListTunnelsAsync(host, default)); Check(prompts == 0 && calls == 1, "permission error retried"); }
                else { await service.ListTunnelsAsync(host, default); Check(prompts == 1 && calls == 2, "expired login not resumed"); }
            }
        });
        yield return ("MSLFRP login waiting is cancellable before cloud writes", async () =>
        {
            var scenario = new Scenario(); using var service = scenario.Service(); using var cancel = new CancellationTokenSource();
            var host = Host with { MslFrpToken = null, LoginMslFrp = ct => new TaskCompletionSource<string>().Task.WaitAsync(ct) };
            var task = service.CreateAsync(Input with { ImportToPanel = false }, host, cancel.Token); cancel.Cancel();
            try { await task; throw new Exception("cancellation ignored"); } catch (OperationCanceledException) { }
            Check(scenario.CloudWrites == 0, "write occurred before login");
        });
        yield return ("MSLFRP node picker excludes offline nodes and creation honors selected node", async () =>
        {
            var scenario = new Scenario(); using var service = scenario.Service(); var calls = 0;
            var host = Host with { SelectMslFrpNode = (nodes, _) => { calls++; Check(nodes.Count == 1 && !nodes.ToJsonString().Contains("secret"), "unsafe options"); return Task.FromResult<object>(new { nodeId = 7 }); } };
            Check(JsonSerializer.Serialize(await service.SelectNodeAsync(host, default)).Contains("7") && calls == 1, "selection lost");
            scenario.Node["status"] = 0; await Fails(() => service.SelectNodeAsync(host, default)); Check(calls == 1, "offline node offered");
            await Fails(() => service.CreateAsync(Input, host with { SelectedMslFrpNodeId = 9 }, default)); Check(scenario.CloudWrites == 0, "different node created");
        });
        yield return ("MSLFRP tools enforce administrator scope and sensitive-operation approval", async () =>
        {
            var names = new[] { "list_mslfrp_nodes", "list_mslfrp_tunnels", "create_mslfrp_tunnel", "import_mslfrp_tunnel", "select_mslfrp_node" };
            var tools = new MslxToolService(null!, null!, null!, new("user", false, "scope"), Host, null!, null!, null!);
            using var args = JsonDocument.Parse("{}");
            foreach (var name in names)
            {
                Check(!MslxToolService.Definitions(false).ToJsonString().Contains(name), "non-admin tool exposed");
                Check(MslxToolService.Definitions(true).ToJsonString().Contains(name), "admin tool missing");
                await Fails(() => tools.ExecuteAsync(name, args.RootElement, null, default));
            }
            Check(MslxToolService.SensitiveTools.Contains(names[2]) && MslxToolService.SensitiveTools.Contains(names[3]), "write lacks approval");
            var context = new DefaultHttpContext();
            context.Request.Headers["x-mslfrp-token"] = "msl-secret";
            Check(MslxHostRequestContext.Capture(context).MslFrpToken == "msl-secret", "MSL login not captured");
        });
        yield return ("MSLFRP discovery returns only safe account/node/tunnel fields", async () =>
        {
            var scenario = new Scenario { Created = true };
            using var service = scenario.Service();
            var nodes = JsonSerializer.Serialize(await service.ListNodesAsync(Host, default));
            var tunnels = JsonSerializer.Serialize(await service.ListTunnelsAsync(Host, default));
            Check(nodes.Contains("min_open_port") && nodes.Contains("maxTunnelCount") && tunnels.Contains("25565"), "discovery lost useful fields");
            Check(!nodes.Contains("secret") && !tunnels.Contains("secret"), "discovery exposed secrets");
        });
        yield return ("MSLFRP creation uses provider payload then imports on local host without starting", async () =>
        {
            var scenario = new Scenario();
            using var service = scenario.Service();
            var result = await service.CreateAsync(Input, Host, default);
            var receipt = JsonSerializer.Serialize(result.Value);
            Check(result.Ok && receipt.Contains("\"tunnelId\":42") && receipt.Contains("\"imported\":true") && receipt.Contains("\"started\":false"), "creation receipt incorrect");
            Check(scenario.CloudWrites == 1 && scenario.PanelWrites == 1, "creation/import was missing or repeated");
            Check(!receipt.Contains("secret") && !receipt.Contains("auth.token"), "configuration leaked to receipt");
        });
        yield return ("MSLFRP cloud-only creation does not contact panel or fetch configuration", async () =>
        {
            var scenario = new Scenario();
            using var service = scenario.Service();
            var result = await service.CreateAsync(Input with { ImportToPanel = false }, Host with { BaseUrl = null, Token = null }, default);
            Check(result.Ok && scenario.PanelRequests == 0 && scenario.ConfigReads == 0 && scenario.CloudWrites == 1, "cloud-only creation touched panel");
        });
        yield return ("MSLFRP invalid inputs and missing login do not send network requests", async () =>
        {
            var scenario = new Scenario();
            using var service = scenario.Service();
            foreach (var input in new[]
            {
                Input with { LocalPort = 0 }, Input with { RemotePort = 65536 }, Input with { Name = "bad\nname" },
                Input with { NodeId = -1 }, Input with { Type = "exec" }, Input with { Protocol = "invalid" },
                Input with { LocalIp = "http://localhost:25565" }, Input with { Type = "http" },
                Input with { Type = "https", BindDomain = "https://example.com/path" }
            }) await Fails(() => service.CreateAsync(input, Host, default));
            await Fails(() => service.ListNodesAsync(Host with { MslFrpToken = null }, default), "登录");
            await Fails(() => service.ListNodesAsync(Host with { MslFrpToken = "bad\nheader" }, default), "登录");
            Check(scenario.Requests == 0, "invalid request reached network");
        });
        yield return ("MSLFRP offline/unsupported nodes and out-of-range ports cannot create tunnels", async () =>
        {
            foreach (var (field, value, input) in new (string, int, MslFrpCreateRequest)[]
            {
                ("status", 0, Input), ("udp_support", 0, Input with { Type = "udp" }),
                ("http_support", 0, Input with { Type = "https", BindDomain = "example.com" }),
                ("kcp_support", 0, Input with { Protocol = "kcp" }),
                ("wss_support", 0, Input with { Protocol = "wss" }),
                ("min_open_port", 31000, Input), ("max_open_port", 30000, Input)
            })
            {
                var scenario = new Scenario(); scenario.Node[field] = value;
                using var service = scenario.Service();
                await Fails(() => service.CreateAsync(input, Host, default));
                Check(scenario.CloudWrites == 0 && scenario.PanelWrites == 0, "unsupported tunnel was created");
            }
        });
        yield return ("MSLFRP HTTP and accelerated transport payloads preserve requested values", async () =>
        {
            var scenario = new Scenario { Expected = Input with { Type = "https", Protocol = "kcp", BindDomain = "example.com" } };
            using var service = scenario.Service();
            Check((await service.CreateAsync(scenario.Expected, Host, default)).Ok, "supported HTTPS/KCP tunnel failed");
        });
        yield return ("MSLFRP rejects duplicate cloud names and skips previously imported tunnels", async () =>
        {
            var scenario = new Scenario { Created = true, Imported = true };
            using var service = scenario.Service();
            await Fails(() => service.CreateAsync(Input, Host, default), "同名");
            var result = await service.ImportAsync(42, Host, default);
            Check(result.Ok && JsonSerializer.Serialize(result.Value).Contains("\"alreadyImported\":true"), "existing import not recognized");
            Check(scenario.CloudWrites == 0 && scenario.PanelWrites == 0 && scenario.ConfigReads == 0, "duplicate created or secret unnecessarily fetched");
            await Fails(() => service.ImportAsync(99, Host, default), "不存在");
            Check(scenario.ConfigReads == 0, "unowned tunnel config requested");
        });
        yield return ("MSLFRP partial import failure retains cloud ID and can resume without recreation", async () =>
        {
            var scenario = new Scenario { FailImport = true };
            using var service = scenario.Service();
            var result = await service.CreateAsync(Input, Host, default);
            var receipt = JsonSerializer.Serialize(result.Value);
            Check(!result.Ok && receipt.Contains("\"created\":true") && receipt.Contains("\"tunnelId\":42") && receipt.Contains("import_mslfrp_tunnel"), "partial result lost");
            Check(!receipt.Contains("secret"), "raw local error/config leaked");
            scenario.FailImport = false;
            Check((await service.ImportAsync(42, Host, default)).Ok, "resume import failed");
            Check(scenario.CloudWrites == 1 && scenario.PanelWrites == 2, "resume created duplicate cloud tunnel");
        });
        yield return ("MSLFRP uncertain creation responses are not retried", async () =>
        {
            foreach (var mode in new[] { "http", "json", "disconnect", "provider" })
            {
                var scenario = new Scenario { CreateFailure = mode };
                using var service = scenario.Service();
                var message = await Fails(() => service.CreateAsync(Input, Host, default));
                Check(scenario.CloudWrites == 1 && scenario.PanelWrites == 0, "write was retried or imported despite failure");
                Check(!message.Contains("secret"), "upstream credentials leaked");
            }
        });
        yield return ("MSLFRP acknowledged creation survives cancellation and post-create discovery failure", async () =>
        {
            foreach (var cancel in new[] { true, false })
            {
                using var cancellation = new CancellationTokenSource();
                var scenario = new Scenario { AfterCreate = cancel ? cancellation.Cancel : null, FailDiscoveryAfterCreate = !cancel };
                using var service = scenario.Service();
                var result = await service.CreateAsync(Input, Host, cancellation.Token);
                Check(!result.Ok && JsonSerializer.Serialize(result.Value).Contains("\"created\":true"), "acknowledged creation lost");
                Check(scenario.CloudWrites == 1 && scenario.PanelWrites == 0, "canceled/uncertain work repeated");
            }
        });
    }

    private sealed class Scenario
    {
        public MslFrpCreateRequest Expected = Input;
        public JsonObject Node = JsonNode.Parse("""{"id":7,"node":"Test","status":1,"min_open_port":30000,"max_open_port":40000,"udp_support":1,"http_support":1,"kcp_support":1,"wss_support":1,"token":"node-secret"}""")!.AsObject();
        public bool Created, Imported, FailImport, FailDiscoveryAfterCreate;
        public string? CreateFailure;
        public Action? AfterCreate;
        public int Requests, CloudWrites, PanelWrites, ConfigReads, PanelRequests;
        public MslFrpService Service() => new(new HttpClient(new Handler(Send)));
        private object Tunnel() => new { id = 42, node_id = Expected.NodeId, name = Expected.Name, type = Expected.Type,
            local_ip = Expected.LocalIp, local_port = Expected.LocalPort, remote_port = Expected.RemotePort,
            protocol = Expected.Protocol, token = "tunnel-secret", config = "config-secret" };
        private async Task<HttpResponseMessage> Send(HttpRequestMessage message)
        {
            Requests++;
            var uri = message.RequestUri!;
            if (uri.Host == "panel.test")
            {
                PanelRequests++;
                Check(message.Headers.GetValues("x-user-token").Single() == "panel-secret", "panel authentication lost");
                Check(message.Headers.Authorization is null && !message.Headers.Contains("x-node-id") && !message.Headers.Contains("x-mslfrp-token"), "cloud credential or stale routing sent to panel");
                if (uri.AbsolutePath == "/api/frp/list")
                    return Packet(Imported ? new object[] { new { id = 3, name = "Minecraft | MSLFRP #42", service = "MSLFrp", status = false } } : System.Array.Empty<object>());
                Check(uri.AbsolutePath == "/api/frp/add" && message.Method == HttpMethod.Post, "unexpected panel operation (possibly start)");
                PanelWrites++;
                var payload = await message.Content!.ReadFromJsonAsync<JsonObject>();
                Check(payload!["config"]!.GetValue<string>().Contains("config-secret") && payload["provider"]!.GetValue<string>() == "MSLFrp"
                    && payload["format"]!.GetValue<string>() == "toml", "panel import payload incorrect");
                if (FailImport) return Packet(null, 500, "config-secret auth.token");
                Imported = true;
                return Packet(null);
            }
            Check(uri.GetLeftPart(UriPartial.Authority) == "https://user.mslmc.net", "credential sent outside MSL");
            Check(message.Headers.Authorization?.ToString() == "Bearer msl-secret" && !message.Headers.Contains("x-user-token"), "cloud authentication leaked/missing");
            switch (uri.AbsolutePath)
            {
                case "/api/frp/userInfo": return Packet(new { maxTunnelCount = 5, user_group_name = "free", token = "account-secret" });
                case "/api/frp/nodeList": return Packet(new[] { Node });
                case "/api/frp/getTunnelList":
                    if (Created && FailDiscoveryAfterCreate) return new(HttpStatusCode.ServiceUnavailable);
                    return Packet(Created ? new[] { Tunnel() } : System.Array.Empty<object>());
                case "/api/frp/getTunnelConfig":
                    Check(uri.Query == "?id=42", "wrong cloud tunnel"); ConfigReads++;
                    return Packet("serverAddr = \"test\"\nauth.token = \"config-secret\"");
                case "/api/frp/addTunnel":
                    CloudWrites++;
                    Check(message.Method == HttpMethod.Post, "creation was not POST");
                    var payload = await message.Content!.ReadFromJsonAsync<JsonObject>();
                    Check(payload!["id"]!.GetValue<int>() == Expected.NodeId && payload["local_port"]!.GetValue<string>() == "25565"
                        && payload["remote_port"]!.GetValue<string>() == "30001" && payload["type"]!.GetValue<string>() == Expected.Type
                        && payload["protocol"]!.GetValue<string>() == Expected.Protocol && payload["bind_domain"]!.GetValue<string>() == Expected.BindDomain
                        && payload["use_kcp"]!.GetValue<bool>() == (Expected.Protocol == "kcp"), "provider payload incorrect");
                    Created = true; AfterCreate?.Invoke();
                    return CreateFailure switch
                    {
                        "http" => new(HttpStatusCode.BadGateway),
                        "json" => new(HttpStatusCode.OK) { Content = new StringContent("invalid") },
                        "disconnect" => throw new HttpRequestException("msl-secret"),
                        "provider" => Packet(null, 400, "msl-secret panel-secret quota exceeded"),
                        _ => Packet(null)
                    };
                default: throw new Exception("unexpected endpoint " + uri.AbsolutePath);
            }
        }
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
    private static HttpResponseMessage Packet(object? data, int code = 200, string msg = "OK") =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(new { code, data, msg }) };
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task<string> Fails(Func<Task> action, string? expected = null)
    {
        try { await action(); }
        catch (ToolException error)
        {
            if (expected is not null) Check(error.Message.Contains(expected), "wrong error: " + error.Message);
            return error.Message;
        }
        throw new Exception("Expected ToolException");
    }
}
