namespace MSLX.Plugin.ElementsAI.Services;

public static class ModelEndpoint
{
    private const string ChatPath = "/chat/completions";

    public static string NormalizeBase(string? endpoint)
    {
        var value = endpoint?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || value.Length > 2048)
            throw new AiValidationException("模型接口地址无效。");

        value = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        // Existing configurations stored the full API path; retain their base path when upgrading.
        if (uri.AbsolutePath.TrimEnd('/').EndsWith(ChatPath, StringComparison.OrdinalIgnoreCase))
            value = value[..^ChatPath.Length].TrimEnd('/');
        return value;
    }

    public static string ChatCompletions(string endpoint) => NormalizeBase(endpoint) + ChatPath;
}
