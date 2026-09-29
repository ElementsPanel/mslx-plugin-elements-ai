using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MSLX.Plugin.ElementsAI.Models;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed class OpenAiProvider
{
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(45);
    private static readonly int[] RetryDelays = [1000, 2000, 4000, 8000, 16000];

    public async Task<ProviderCompletion> CompleteAsync(
        ResolvedModel model,
        IReadOnlyList<ProviderMessage> messages,
        JsonArray tools,
        Func<string, Task> onDelta,
        Func<string, Task> onReasoning,
        Func<int, int, Task> onRetry,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var publicAddresses = model.PublicOnly
                    ? await ResolvePublicEndpointAsync(model.Endpoint, cancellationToken)
                    : null;
                return await CompleteOnceAsync(model, messages, tools, publicAddresses, onDelta, onReasoning, cancellationToken);
            }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = error;
                if (!IsRetryable(error, cancellationToken) || attempt >= RetryDelays.Length)
                    throw new ProviderException(SafeError(error));
                var delay = RetryDelays[attempt];
                await onRetry(attempt + 1, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
        throw new ProviderException(SafeError(lastError));
    }

    private static async Task<ProviderCompletion> CompleteOnceAsync(
        ResolvedModel model,
        IReadOnlyList<ProviderMessage> messages,
        JsonArray tools,
        IPAddress[]? publicAddresses,
        Func<string, Task> onDelta,
        Func<string, Task> onReasoning,
        CancellationToken cancellationToken)
    {
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            UseCookies = false,
            UseProxy = publicAddresses is null
        };
        if (publicAddresses is not null)
        {
            handler.ConnectCallback = async (context, token) =>
            {
                Exception? last = null;
                foreach (var address in publicAddresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception error)
                    {
                        last = error;
                        socket.Dispose();
                    }
                }
                throw new HttpRequestException("无法连接个人模型接口。", last);
            };
        }
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(HttpMethod.Post, model.Endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.UserAgent.ParseAdd("MSLX-Elements-AI/0.1.8");
        if (!string.IsNullOrEmpty(model.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", model.ApiKey);

        var body = new JsonObject
        {
            ["model"] = model.Model,
            ["stream"] = true,
            ["messages"] = JsonSerializer.SerializeToNode(messages),
            ["tools"] = tools.DeepClone(),
            ["tool_choice"] = "auto"
        };
        if (model.ThinkingEnabled.HasValue)
        {
            body["enable_thinking"] = model.ThinkingEnabled.Value;
            if (model.ThinkingEnabled.Value) body["thinking_effort"] = model.ThinkingEffort;
        }
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AttemptTimeout);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            var detail = (await response.Content.ReadAsStringAsync(timeout.Token)).Trim();
            if (detail.Length > 500) detail = detail[..500];
            throw new ProviderHttpException((int)response.StatusCode, detail);
        }
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!mediaType.Contains("event-stream", StringComparison.OrdinalIgnoreCase))
            throw new ProviderException("模型接口没有返回 SSE 流。");

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: false);
        var completion = new ProviderCompletion();
        var calls = new Dictionary<int, ProviderToolCall>();
        var dataLines = new List<string>();
        while (true)
        {
            var line = await reader.ReadLineAsync(timeout.Token);
            if (line is null) break;
            if (line.Length == 0)
            {
                if (dataLines.Count == 0) continue;
                var data = string.Join("\n", dataLines);
                dataLines.Clear();
                if (data == "[DONE]") break;
                await ApplyEventAsync(data, completion, calls, onDelta, onReasoning);
                continue;
            }
            if (line.StartsWith("data:", StringComparison.Ordinal))
                dataLines.Add(line[5..].TrimStart());
        }
        if (dataLines.Count > 0)
        {
            var data = string.Join("\n", dataLines);
            if (data != "[DONE]") await ApplyEventAsync(data, completion, calls, onDelta, onReasoning);
        }
        completion.ToolCalls = calls.OrderBy(item => item.Key).Select(item => item.Value).ToList();
        return completion;
    }

    private static async Task ApplyEventAsync(
        string data,
        ProviderCompletion completion,
        Dictionary<int, ProviderToolCall> calls,
        Func<string, Task> onDelta,
        Func<string, Task> onReasoning)
    {
        using var document = JsonDocument.Parse(data);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return;
        var choice = choices[0];
        if (!choice.TryGetProperty("delta", out var delta)) return;
        if (TryText(delta, "content", out var content) && content.Length > 0)
        {
            completion.Content += content;
            await onDelta(content);
        }
        if ((TryText(delta, "reasoning_content", out var reasoning) || TryText(delta, "reasoning", out reasoning)) &&
            reasoning.Length > 0)
        {
            completion.Reasoning += reasoning;
            await onReasoning(reasoning);
        }
        if (!delta.TryGetProperty("tool_calls", out var toolCalls) || toolCalls.ValueKind != JsonValueKind.Array)
            return;
        foreach (var item in toolCalls.EnumerateArray())
        {
            var index = item.TryGetProperty("index", out var indexValue) ? indexValue.GetInt32() : calls.Count;
            if (!calls.TryGetValue(index, out var call))
            {
                call = new ProviderToolCall();
                calls[index] = call;
            }
            if (TryText(item, "id", out var id)) call.Id += id;
            if (!item.TryGetProperty("function", out var function)) continue;
            if (TryText(function, "name", out var name)) call.Function.Name += name;
            if (TryText(function, "arguments", out var arguments)) call.Function.Arguments += arguments;
        }
    }

    private static bool TryText(JsonElement element, string property, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(property, out var token) || token.ValueKind != JsonValueKind.String)
            return false;
        value = token.GetString() ?? string.Empty;
        return true;
    }

    private static bool IsRetryable(Exception error, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return false;
        if (error is ProviderHttpException http)
            return http.StatusCode is 408 or 409 or 429 or >= 500 and <= 599;
        return error is HttpRequestException or IOException or TaskCanceledException;
    }

    private static string SafeError(Exception? error)
    {
        var message = error switch
        {
            ProviderHttpException http => $"HTTP {http.StatusCode}: {http.Message}",
            ProviderException provider => provider.Message,
            HttpRequestException http => http.HttpRequestError.ToString(),
            TaskCanceledException => "模型请求超时。",
            _ => error?.Message ?? "模型请求失败。"
        };
        return message.Length > 500 ? message[..500] : message;
    }

    private static async Task<IPAddress[]> ResolvePublicEndpointAsync(string endpoint, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new ProviderException("个人模型接口地址无效。");
        if (uri.HostNameType == UriHostNameType.Dns &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)))
            throw new ProviderException("普通用户的个人模型不能访问本机或内网地址。");

        IPAddress[] addresses;
        try
        {
            addresses = uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
                ? [IPAddress.Parse(uri.Host)]
                : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch
        {
            throw new ProviderException("无法解析个人模型接口地址。");
        }
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new ProviderException("普通用户的个人模型不能访问本机或内网地址。");
        return addresses;
    }

    private static bool IsPublic(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;
        if (address.IsIPv4MappedToIPv6) return IsPublic(address.MapToIPv4());
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] != 0 && bytes[0] != 10 && bytes[0] != 127 &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] is 0 or 168) &&
                   !(bytes[0] == 198 && bytes[1] is 18 or 19 or 51) &&
                   !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) &&
                   !(bytes[0] >= 224);
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None) ||
                address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
                return false;
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xfe) == 0xfc) return false;
            return !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
        }
        return false;
    }
}

public class ProviderException(string message) : Exception(message);

public sealed class ProviderHttpException(int statusCode, string message) : ProviderException(message)
{
    public int StatusCode { get; } = statusCode;
}
