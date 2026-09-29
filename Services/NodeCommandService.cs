using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.SDK;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed class NodeCommandService(HttpClient client) : IDisposable
{
    public const string Route = "api/plugins/mslx-plugin-elements-ai/node-command";
    private const int MaxOutputChars = 16000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Dispose() => client.Dispose();

    public static void RequireAdmin(string userId)
    {
        var current = SDK.MSLX.Config.Users.GetUserById(userId);
        if (current is null || !current.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            throw new ToolException("仅管理员可以执行节点工具。");
    }

    public async Task<List<NodeOption>> ListNodesAsync(MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        var nodes = await ReadNodesAsync(host, cancellationToken);
        return [new("local", "本机节点", true), .. nodes.Select(node => new NodeOption(node.NodeId, node.NodeName, false))];
    }

    public async Task<NodeCommandResult> ExecuteAsync(
        string nodeId, NodeCommandRequest request, MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        Validate(request);
        if (nodeId == "local") return await ExecuteLocalAsync(request, cancellationToken);

        // Only registered addresses can receive the user's authentication token.
        var nodes = await ReadNodesAsync(host, cancellationToken);
        var node = nodes.SingleOrDefault(node => node.NodeId == nodeId)
                   ?? throw new ToolException("节点不存在，请先查询节点列表。");
        if (!Uri.TryCreate(node.NodeUrl.TrimEnd('/') + "/" + Route, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ToolException("节点地址无效，请检查 MSLX 节点配置。");

        using var message = AuthenticatedRequest(HttpMethod.Post, uri, host);
        message.Headers.Add("x-node-id", node.NodeId);
        message.Content = JsonContent.Create(request);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds + 15));
        try
        {
            var data = await SendAsync(message, timeout.Token);
            var result = data.Deserialize<NodeCommandResult>(JsonOptions)
                         ?? throw new ToolException("节点没有返回命令执行结果。");
            if (result.Stdout is null || result.Stderr is null || string.IsNullOrWhiteSpace(result.WorkingDirectory))
                throw new ToolException("节点返回了不完整的执行结果，请核实执行状态。");
            result.Truncated |= result.Stdout.Length > MaxOutputChars || result.Stderr.Length > MaxOutputChars;
            if (result.Stdout.Length > MaxOutputChars) result.Stdout = result.Stdout[..MaxOutputChars];
            if (result.Stderr.Length > MaxOutputChars) result.Stderr = result.Stderr[..MaxOutputChars];
            result.NodeId = node.NodeId;
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ToolException("等待节点响应超时，执行状态未知；请先核实结果，勿重复执行命令。");
        }
        catch (HttpRequestException)
        {
            throw new ToolException("节点连接中断，执行状态未知；请先核实结果，勿重复执行命令。");
        }
        catch (JsonException)
        {
            throw new ToolException("节点返回了不完整的执行结果，请核实执行状态。");
        }
    }

    public static async Task<NodeCommandResult> ExecuteLocalAsync(NodeCommandRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        cancellationToken.ThrowIfCancellationRequested();
        var directory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
            ? AppContext.BaseDirectory
            : request.WorkingDirectory;
        if (!Path.IsPathFullyQualified(directory) || !Directory.Exists(directory))
            throw new ToolException("工作目录必须是节点上已存在的绝对路径。");

        var start = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (OperatingSystem.IsWindows())
        {
            // cmd /s /c strips the outer quotes; C runtime ArgumentList escaping
            // would instead insert backslashes into commands containing quotes.
            start.Arguments = "/s /d /c \"" + request.Command + "\"";
        }
        else
        {
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(request.Command);
        }

        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new ToolException("无法启动节点命令。");
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            throw new ToolException("无法启动节点 Shell，请检查运行环境和权限。");
        }

        process.StandardInput.Close();
        var output = new BoundedOutput();
        var errors = new BoundedOutput();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        var stdout = output.ReadAsync(process.StandardOutput, timeout.Token);
        var stderr = errors.ReadAsync(process.StandardError, timeout.Token);
        var timedOut = false;
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(timeout.Token), stdout, stderr).WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = !cancellationToken.IsCancellationRequested;
        }
        finally
        {
            // Cancel the readers as descendants may still hold the output pipes open.
            await timeout.CancelAsync();
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
            await Task.WhenAll(stdout, stderr);
            await process.WaitForExitAsync();
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new NodeCommandResult
        {
            WorkingDirectory = directory,
            ExitCode = process.ExitCode,
            Stdout = output.Text,
            Stderr = errors.Text,
            TimedOut = timedOut,
            Truncated = output.Truncated || errors.Truncated
        };
    }

    private async Task<List<RegisteredNode>> ReadNodesAsync(MslxHostRequestContext host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host.BaseUrl)) throw new ToolException("无法确定 MSLX 本地地址。");
        using var message = AuthenticatedRequest(HttpMethod.Get, new Uri(host.BaseUrl.TrimEnd('/') + "/api/node/list"), host);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var data = await SendAsync(message, timeout.Token);
            var nodes = data.Deserialize<List<RegisteredNode>>(JsonOptions);
            if (nodes is null || nodes.Any(node => node is null || string.IsNullOrWhiteSpace(node.NodeId)
                    || node.NodeId == "local" || string.IsNullOrWhiteSpace(node.NodeUrl))
                || nodes.Select(node => node.NodeId).Distinct(StringComparer.Ordinal).Count() != nodes.Count)
                throw new ToolException("MSLX 未返回有效的节点列表。");
            return nodes;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ToolException("读取节点列表超时。");
        }
        catch (HttpRequestException)
        {
            throw new ToolException("无法读取 MSLX 节点列表。");
        }
        catch (JsonException)
        {
            throw new ToolException("MSLX 未返回有效的节点列表。");
        }
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, Uri uri, MslxHostRequestContext host)
    {
        if (string.IsNullOrWhiteSpace(host.Token)) throw new ToolException("未提供 MSLX 认证凭证。");
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("x-user-token", host.Token);
        return message;
    }

    private async Task<JsonElement> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new ToolException("节点接口不存在，请确认目标节点已安装并启用新版 Elements AI 插件。");
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new ToolException("节点拒绝访问，请检查管理员权限和节点连接状态。");
        if (!response.IsSuccessStatusCode)
            throw new ToolException($"节点请求失败（HTTP {(int)response.StatusCode}），请核实执行状态后再操作。");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + count > 512 * 1024) throw new ToolException("节点响应超过长度限制。");
            buffer.Write(chunk, 0, count);
        }
        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray());
            var root = document.RootElement;
            if (root.GetProperty("code").GetInt32() != 200)
                throw new ToolException("节点请求未成功，请检查节点日志并核实执行状态。");
            return root.GetProperty("data").Clone();
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ToolException("节点返回了无效响应，请确认目标节点已启用新版 Elements AI 插件。");
        }
    }

    private static void Validate(NodeCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command) || request.Command.Length > 4096 || request.Command.Contains('\0'))
            throw new ToolException("命令必须为 1 到 4096 个字符，且不能包含空字符。");
        if (request.TimeoutSeconds is < 1 or > 120)
            throw new ToolException("命令超时时间必须为 1 到 120 秒。");
        if (request.WorkingDirectory is { Length: > 2048 } || request.WorkingDirectory?.Contains('\0') == true)
            throw new ToolException("工作目录无效。");
    }

    private sealed class RegisteredNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string NodeUrl { get; set; } = string.Empty;
    }

    private sealed class BoundedOutput
    {
        private readonly StringBuilder _text = new();
        public string Text => _text.ToString();
        public bool Truncated { get; private set; }

        public async Task ReadAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            var buffer = new char[4096];
            try
            {
                int count;
                while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    var keep = Math.Min(count, MaxOutputChars - _text.Length);
                    _text.Append(buffer, 0, keep);
                    if (keep < count) Truncated = true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }
    }
}
