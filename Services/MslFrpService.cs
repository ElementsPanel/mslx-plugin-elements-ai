using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using MSLX.Plugin.ElementsAI.Models;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed record MslFrpCreateRequest
{
    public int NodeId { get; init; }
    public string Name { get; init; } = "";
    public string Type { get; init; } = "tcp";
    public string LocalIp { get; init; } = "127.0.0.1";
    public int LocalPort { get; init; }
    public int RemotePort { get; init; }
    public string Protocol { get; init; } = "tcp";
    public string BindDomain { get; init; } = "";
    public bool ImportToPanel { get; init; } = true;
}

/// <summary>Uses the panel's MSL login for this request only; credentials/configs never become tool results.</summary>
public sealed class MslFrpService(HttpClient client) : IDisposable
{
    private const string ApiBase = "https://user.mslmc.net";
    public void Dispose() => client.Dispose();

    public async Task<object> ListPanelTunnelsAsync(MslxHostRequestContext host, CancellationToken cancellationToken) =>
        (await PanelListAsync(host, cancellationToken)).Select(n => Project(n, "id", "name", "service", "status")).ToArray();

    public async Task<ToolExecutionResult> StartPanelTunnelAsync(int id, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        if (id <= 0) throw new ToolException("面板隧道 ID 必须为正整数。");
        var tunnel = (await PanelListAsync(host, cancellationToken)).SingleOrDefault(n => Number(n, "id") == id)
            ?? throw new ToolException("本机面板中不存在该隧道，请先查询 list_tunnels，勿使用云端隧道 ID。");
        if (Running(tunnel) == true) return new() { Value = new { panelTunnelId = id, panelNodeId = "local", running = true, alreadyRunning = true } };
        await SendAsync("/api/frp/action", host, cancellationToken, new { id, action = "start" }, panel: true);
        var result = new JsonObject { ["panelTunnelId"] = id, ["panelNodeId"] = "local", ["startAccepted"] = true, ["verified"] = false };
        try
        {
            var current = (await PanelListAsync(host, cancellationToken)).SingleOrDefault(n => Number(n, "id") == id);
            result["running"] = Running(current);
            result["verified"] = current is not null && Running(current).HasValue;
        }
        catch (Exception error) when (error is ToolException or OperationCanceledException)
        { result["nextAction"] = "启动指令已接受，状态尚未确认，请查询 list_tunnels。"; }
        return new() { Value = result };
    }

    public async Task<ToolExecutionResult> DeleteTunnelAsync(int id, bool panel, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        if (id <= 0) throw new ToolException("隧道 ID 必须为正整数。");
        var before = panel ? await PanelListAsync(host, cancellationToken) : await TunnelsAsync(host, cancellationToken);
        var tunnel = before.SingleOrDefault(n => Number(n, "id") == id)
            ?? throw new ToolException(panel ? "本机面板隧道不存在，请查询 list_tunnels。" : "当前 MSL 账号下的云端隧道不存在，请查询 list_mslfrp_tunnels。");
        await SendAsync(panel ? "/api/frp/delete" : "/api/frp/deleteTunnel", host, cancellationToken, new { id }, panel);
        var result = new JsonObject { [panel ? "panelTunnelId" : "tunnelId"] = id,
            ["scope"] = panel ? "local_panel" : "mslfrp_cloud", ["deleteAccepted"] = true, ["verified"] = false };
        try
        {
            var after = panel ? await PanelListAsync(host, cancellationToken) : await TunnelsAsync(host, cancellationToken);
            result["deleted"] = !after.Any(n => Number(n, "id") == id);
            result["verified"] = true;
        }
        catch (Exception error) when (error is ToolException or OperationCanceledException)
        { result["nextAction"] = "删除指令已接受，请查询对应隧道列表确认结果，勿盲目重复删除。"; }
        return new() { Value = result };
    }

    private static bool? Running(JsonNode? tunnel) => tunnel?["status"] is JsonValue v && v.TryGetValue<bool>(out var running) ? running : null;

    public async Task<object> ListNodesAsync(MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        var account = await CloudAsync("userInfo", host, cancellationToken);
        var nodes = await NodesAsync(host, cancellationToken);
        return new
        {
            account = Project(account, "user_group_name", "maxTunnelCount", "boundLimit", "realNameStatus", "outdated"),
            nodes = nodes.Select(node => Project(node, "id", "node", "domain", "min_open_port", "max_open_port",
                "udp_support", "http_support", "kcp_support", "wss_support", "need_real_name", "bandwidth",
                "allow_user_group", "remarks", "status")).ToArray()
        };
    }

    public async Task<object> SelectNodeAsync(MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.SerializeToNode(await ListNodesAsync(host, cancellationToken))!;
        var nodes = new JsonArray(data["nodes"]!.AsArray().Where(n => Number(n, "status") == 1)
            .Select(n => n!.DeepClone()).ToArray());
        if (nodes.Count == 0) throw new ToolException("当前账号没有可用的在线 MSLFRP 节点。");
        if (host.SelectMslFrpNode is null) throw new ToolException("当前请求不支持节点选择。");
        return await host.SelectMslFrpNode(nodes, cancellationToken);
    }

    public async Task<object> ListTunnelsAsync(MslxHostRequestContext host, CancellationToken cancellationToken) =>
        (await TunnelsAsync(host, cancellationToken)).Select(SafeTunnel).ToArray();

    public async Task<ToolExecutionResult> CreateAsync(
        MslFrpCreateRequest input, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        Validate(input);
        if (host.SelectedMslFrpNodeId is int selected && input.NodeId != selected)
            throw new ToolException($"请使用用户选择的 MSLFRP 节点 #{selected} 创建隧道，或重新请用户选择节点。");
        // Check panel access before creating a cloud resource when import was requested.
        if (input.ImportToPanel) await PanelListAsync(host, cancellationToken);
        var nodes = await NodesAsync(host, cancellationToken);
        var node = nodes.SingleOrDefault(item => Number(item, "id") == input.NodeId)
                   ?? throw new ToolException("MSLFRP 节点不存在，请先查询节点列表。");
        ValidateNode(input, node);
        var before = await TunnelsAsync(host, cancellationToken);
        if (before.Any(item => Text(item, "name") == input.Name))
            throw new ToolException("MSLFRP 已有同名隧道，请查询隧道列表；如需导入，请使用现有隧道 ID，勿重复创建。");
        var payload = new
        {
            id = input.NodeId, name = input.Name, type = input.Type, local_ip = input.LocalIp,
            local_port = input.LocalPort.ToString(CultureInfo.InvariantCulture),
            remote_port = input.RemotePort.ToString(CultureInfo.InvariantCulture),
            remarks = "Created by Elements AI", bind_domain = input.BindDomain,
            protocol = input.Protocol, use_kcp = input.Protocol == "kcp", extra_config = (string?)null
        };
        // Never retry a write. Preserve an acknowledged result even if the chat is stopped during submission.
        await SendAsync("/api/frp/addTunnel", host, cancellationToken, payload);
        var result = new JsonObject
        {
            ["created"] = true, ["imported"] = false, ["started"] = false,
            ["name"] = input.Name, ["nodeId"] = input.NodeId,
            ["type"] = input.Type, ["localIp"] = input.LocalIp,
            ["localPort"] = input.LocalPort, ["remotePort"] = input.RemotePort
        };
        try
        {
            var previousIds = before.Select(item => Number(item, "id")).ToHashSet();
            var matches = (await TunnelsAsync(host, cancellationToken)).Where(item =>
                !previousIds.Contains(Number(item, "id")) && Matches(input, item)).ToArray();
            if (matches.Length != 1)
                throw new ToolException("云端已确认创建，但尚未确定新隧道 ID。请查询隧道列表核实，勿重复创建。");
            var tunnel = matches[0]!;
            var id = Number(tunnel, "id");
            if (id <= 0) throw new ToolException("云端已确认创建，但返回的隧道 ID 无效。请查询隧道列表，勿重复创建。");
            result["tunnelId"] = id;
            if (input.ImportToPanel)
            {
                var imported = await ImportKnownAsync(tunnel, host, cancellationToken);
                foreach (var pair in imported) result[pair.Key] = pair.Value?.DeepClone();
            }
            return new() { Value = result };
        }
        catch (Exception error) when (error is ToolException or OperationCanceledException)
        {
            result["error"] = error is ToolException ? error.Message : "云端已创建，后续查询或导入已停止。";
            result["nextAction"] = "查询 list_mslfrp_tunnels 核实；需要导入时使用 import_mslfrp_tunnel，勿再次创建。";
            return new() { Ok = false, Value = result };
        }
    }

    public async Task<ToolExecutionResult> ImportAsync(int tunnelId, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        if (tunnelId <= 0) throw new ToolException("隧道 ID 必须为正整数。");
        var tunnel = (await TunnelsAsync(host, cancellationToken)).SingleOrDefault(item => Number(item, "id") == tunnelId)
                     ?? throw new ToolException("当前 MSL 账号下不存在该隧道，请先查询隧道列表。");
        return new() { Value = await ImportKnownAsync(tunnel, host, cancellationToken) };
    }

    private async Task<JsonObject> ImportKnownAsync(JsonNode tunnel, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        var id = Number(tunnel, "id");
        var name = $"{Text(tunnel, "name")} | MSLFRP #{id}";
        var existing = (await PanelListAsync(host, cancellationToken))
            .FirstOrDefault(item => Text(item, "service") == "MSLFrp" && Text(item, "name") == name);
        var result = new JsonObject
        {
            ["tunnelId"] = id, ["imported"] = true, ["panelNodeId"] = "local", ["panelName"] = name,
            ["started"] = false, ["alreadyImported"] = existing is not null
        };
        if (existing is not null)
        {
            result["panelTunnelId"] = Number(existing, "id");
            result["alreadyRunning"] = existing["status"]?.DeepClone();
            return result;
        }
        var data = await CloudAsync($"getTunnelConfig?id={id}", host, cancellationToken);
        if (data is not JsonValue value || !value.TryGetValue<string>(out var config)
            || string.IsNullOrWhiteSpace(config) || config.Length > 65536)
            throw new ToolException("MSLFRP 未返回有效的隧道配置，未导入面板。");
        await SendAsync("/api/frp/add", host, cancellationToken,
            new { name, config, provider = "MSLFrp", format = "toml" }, panel: true);
        return result;
    }

    private async Task<JsonArray> NodesAsync(MslxHostRequestContext host, CancellationToken cancellationToken) =>
        Array(await CloudAsync("nodeList", host, cancellationToken));
    private async Task<JsonArray> TunnelsAsync(MslxHostRequestContext host, CancellationToken cancellationToken) =>
        Array(await CloudAsync("getTunnelList", host, cancellationToken));
    private async Task<JsonArray> PanelListAsync(MslxHostRequestContext host, CancellationToken cancellationToken) =>
        Array(await SendAsync("/api/frp/list", host, cancellationToken, panel: true));
    private Task<JsonNode?> CloudAsync(string action, MslxHostRequestContext host, CancellationToken cancellationToken) =>
        SendAsync("/api/frp/" + action, host, cancellationToken);

    private async Task<JsonNode?> SendAsync(string path, MslxHostRequestContext host, CancellationToken cancellationToken,
        object? payload = null, bool panel = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!panel && string.IsNullOrWhiteSpace(host.MslFrpToken) && host.LoginMslFrp is not null)
            host.MslFrpToken = await host.LoginMslFrp(cancellationToken);
        var credential = panel ? host.Token : host.MslFrpToken;
        if (string.IsNullOrWhiteSpace(credential) || credential.Length > 8192 || credential.Any(char.IsControl))
            throw new ToolException(panel ? "MSLX 登录状态已失效，请重新登录。" : "请先在面板「创建隧道 → MSLFrp」中登录 MSL 账号，登录后继续。");
        if (panel && (!Uri.TryCreate(host.BaseUrl, UriKind.Absolute, out var hostUri)
                      || hostUri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(hostUri.UserInfo)))
            throw new ToolException("无法确定 MSLX 本地地址，未提交隧道配置。");
        using var message = new HttpRequestMessage(payload is null ? HttpMethod.Get : HttpMethod.Post,
            (panel ? host.BaseUrl!.TrimEnd('/') : ApiBase) + path);
        if (panel) message.Headers.Add("x-user-token", credential);
        else message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        if (payload is not null) message.Content = JsonContent.Create(payload);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(payload is null ? cancellationToken : CancellationToken.None);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var source = panel ? "MSLX" : "MSLFRP";
        var uncertain = payload is null ? "" : "提交状态可能未知，请查询隧道列表核实，勿重复提交。";
        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!panel && response.StatusCode == HttpStatusCode.Unauthorized && host.LoginMslFrp is not null)
            {
                host.MslFrpToken = await host.LoginMslFrp(cancellationToken);
                return await SendAsync(path, host, cancellationToken, payload, panel);
            }
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new ToolException($"{source} 拒绝访问，请检查账号登录状态与权限。");
            if (!response.IsSuccessStatusCode)
                throw new ToolException($"{source} 请求失败（HTTP {(int)response.StatusCode}）。{uncertain}");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + count > 1024 * 1024) throw new ToolException($"{source} 响应过大。{uncertain}");
                buffer.Write(chunk, 0, count);
            }
            var root = JsonNode.Parse(buffer.ToArray()) as JsonObject
                       ?? throw new JsonException();
            if (!panel && Number(root, "code") == 401 && host.LoginMslFrp is not null)
            {
                host.MslFrpToken = await host.LoginMslFrp(cancellationToken);
                return await SendAsync(path, host, cancellationToken, payload, panel);
            }
            if (Number(root, "code") != 200)
            {
                // Local errors may contain a raw FRP configuration. Never forward them.
                var detail = panel ? "请检查面板隧道配置与权限。" : Text(root, "msg", Text(root, "message", "请检查账号配额、节点权限与端口占用。"));
                foreach (var secret in new[] { host.MslFrpToken, host.Token })
                    if (!string.IsNullOrEmpty(secret)) detail = detail.Replace(secret, "[已隐藏]", StringComparison.Ordinal);
                if (detail.Length > 300) detail = detail[..300];
                throw new ToolException($"{source} 请求失败：{detail}{uncertain}");
            }
            return root["data"]?.DeepClone();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested || payload is not null)
        { throw new ToolException($"{source} 请求超时。{uncertain}"); }
        catch (HttpRequestException) { throw new ToolException($"{source} 连接失败。{uncertain}"); }
        catch (IOException) { throw new ToolException($"{source} 响应中断。{uncertain}"); }
        catch (JsonException) { throw new ToolException($"{source} 返回了无效响应。{uncertain}"); }
    }

    private static void Validate(MslFrpCreateRequest input)
    {
        if (input.NodeId <= 0) throw new ToolException("请选择有效的 MSLFRP 节点 ID。");
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 || input.Name.Any(char.IsControl))
            throw new ToolException("隧道名称必须为 1 至 100 个字符，且不能包含控制字符。");
        if (input.Type is not ("tcp" or "udp" or "http" or "https") || input.Protocol is not ("tcp" or "kcp" or "wss"))
            throw new ToolException("隧道类型或传输协议无效。");
        if (string.IsNullOrWhiteSpace(input.LocalIp) || input.LocalIp.Length > 253 || Uri.CheckHostName(input.LocalIp) == UriHostNameType.Unknown)
            throw new ToolException("localIp 必须是有效的 IP 地址或主机名，不能包含协议或端口。");
        if (input.LocalPort is < 1 or > 65535 || input.RemotePort is < 1 or > 65535)
            throw new ToolException("本地端口和远程端口必须为 1 至 65535 的整数。");
        if (input.BindDomain is null || input.BindDomain.Length > 253 || input.BindDomain.Any(char.IsControl)
            || (input.BindDomain.Length > 0 && Uri.CheckHostName(input.BindDomain) != UriHostNameType.Dns)
            || (input.Type is "http" or "https" && input.BindDomain.Length == 0))
            throw new ToolException("HTTP/HTTPS 隧道必须提供有效的 bindDomain 域名（不含协议和路径）。");
    }

    private static void ValidateNode(MslFrpCreateRequest input, JsonNode node)
    {
        if (Number(node, "status") != 1) throw new ToolException("该 MSLFRP 节点当前离线。");
        if (input.RemotePort < Number(node, "min_open_port") || input.RemotePort > Number(node, "max_open_port"))
            throw new ToolException("远程端口超出该 MSLFRP 节点开放范围，请查询节点列表后调整。");
        if ((input.Type == "udp" && Number(node, "udp_support") != 1)
            || (input.Type is "http" or "https" && Number(node, "http_support") != 1)
            || (input.Protocol == "kcp" && Number(node, "kcp_support") != 1)
            || (input.Protocol == "wss" && Number(node, "wss_support") != 1))
            throw new ToolException("该 MSLFRP 节点不支持所选隧道类型或传输协议。");
    }

    private static bool Matches(MslFrpCreateRequest input, JsonNode? tunnel) =>
        Text(tunnel, "name") == input.Name && Number(tunnel, "node_id") == input.NodeId
        && Text(tunnel, "type") == input.Type && Text(tunnel, "local_ip") == input.LocalIp
        && Number(tunnel, "local_port") == input.LocalPort && Number(tunnel, "remote_port") == input.RemotePort;

    private static JsonObject SafeTunnel(JsonNode? tunnel) => Project(tunnel, "id", "node_id", "name", "local_ip",
        "local_port", "remote_port", "type", "status", "protocol", "use_kcp", "bind_domain");
    private static JsonObject Project(JsonNode? node, params string[] fields)
    {
        var result = new JsonObject();
        if (node is not JsonObject obj) return result;
        foreach (var field in fields)
            if (obj[field] is JsonValue value) result[field] = value.DeepClone();
        return result;
    }
    private static JsonArray Array(JsonNode? node) => node is JsonArray array && array.All(item => item is JsonObject)
        ? array : throw new ToolException("隧道接口返回了无效列表。");
    private static string Text(JsonNode? node, string key, string fallback = "") =>
        node?[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : fallback;
    private static int Number(JsonNode? node, string key) =>
        int.TryParse(node?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
