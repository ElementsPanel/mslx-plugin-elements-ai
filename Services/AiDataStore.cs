using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.SDK;
using Newtonsoft.Json.Linq;

namespace MSLX.Plugin.ElementsAI.Services;

public sealed class AiDataStore
{
    private const int MaxPersonalModels = 20;
    private const int MaxPresetModels = 50;
    private const int MaxConversations = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new();
    private readonly SemaphoreSlim _presetLock = new(1, 1);

    private string DataPath
    {
        get
        {
            var path = SDK.MSLX.Config.GetPluginConfig(MSLXPluginEntry.PluginId).GetDataPath();
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.Combine(path, "users"));
            return path;
        }
    }

    public async Task<AiStatus> GetStatusAsync(UserContext user, CancellationToken cancellationToken)
    {
        var models = await ListModelsAsync(user.UserId, cancellationToken);
        var data = await ReadUserAsync(user.UserId, cancellationToken);
        return new AiStatus
        {
            Ready = models.Count > 0,
            Admin = user.IsAdmin,
            UserId = user.UserId,
            Models = models,
            Preferences = data.Preferences
        };
    }

    public async Task<List<ModelOption>> ListModelsAsync(string userId, CancellationToken cancellationToken)
    {
        var presets = await ReadPresetsAsync(cancellationToken);
        var personal = (await ReadUserAsync(userId, cancellationToken)).Models;
        return
        [
            .. presets.Select(model => ToOption(model, "preset")),
            .. personal.Select(model => ToOption(model, "personal"))
        ];
    }

    public async Task<List<ModelOption>> ListPresetsAsync(CancellationToken cancellationToken)
    {
        return (await ReadPresetsAsync(cancellationToken))
            .Select(model => ToOption(model, "preset", includeEndpoint: true))
            .ToList();
    }

    public async Task<ResolvedModel> ResolveModelAsync(
        UserContext user,
        string selectionId,
        CancellationToken cancellationToken)
    {
        var parts = selectionId.Split(':', 2);
        if (parts.Length != 2 || (parts[0] != "preset" && parts[0] != "personal") || !ValidId(parts[1]))
            throw new AiValidationException("所选模型不存在。");

        var models = parts[0] == "preset"
            ? await ReadPresetsAsync(cancellationToken)
            : (await ReadUserAsync(user.UserId, cancellationToken)).Models;
        var model = models.FirstOrDefault(item => item.Id == parts[1])
                    ?? throw new AiValidationException("所选模型不存在。");

        return new ResolvedModel
        {
            Id = model.Id,
            Name = model.Name,
            Endpoint = model.Endpoint,
            Model = model.Model,
            ApiKey = model.ApiKey,
            ThinkingEnabled = model.ThinkingEnabled,
            ThinkingEffort = model.ThinkingEffort,
            SelectionId = selectionId,
            PublicOnly = parts[0] == "personal" && !user.IsAdmin
        };
    }

    public Task SavePersonalModelAsync(
        string userId,
        ModelInput input,
        CancellationToken cancellationToken)
    {
        return WithUserLockAsync(userId, async data =>
        {
            var previous = input.Id is null ? null : data.Models.FirstOrDefault(item => item.Id == input.Id);
            if (input.Id is not null && previous is null) throw new AiValidationException("模型不存在。");
            if (previous is null && data.Models.Count >= MaxPersonalModels)
                throw new AiValidationException($"每个账号最多保存 {MaxPersonalModels} 个个人模型。");

            var saved = ValidateModel(input, previous);
            if (previous is null) data.Models.Add(saved);
            else data.Models[data.Models.IndexOf(previous)] = saved;
            await WriteUserUnlockedAsync(userId, data, cancellationToken);
        }, cancellationToken);
    }

    public Task DeletePersonalModelAsync(string userId, string id, CancellationToken cancellationToken)
    {
        if (!ValidId(id)) throw new AiValidationException("模型 ID 无效。");
        return WithUserLockAsync(userId, async data =>
        {
            var removed = data.Models.RemoveAll(item => item.Id == id);
            if (removed == 0) throw new AiValidationException("模型不存在。");
            await WriteUserUnlockedAsync(userId, data, cancellationToken);
        }, cancellationToken);
    }

    public async Task SavePresetAsync(ModelInput input, CancellationToken cancellationToken)
    {
        await _presetLock.WaitAsync(cancellationToken);
        try
        {
            var presets = ReadPresetsUnlocked();
            var previous = input.Id is null ? null : presets.FirstOrDefault(item => item.Id == input.Id);
            if (input.Id is not null && previous is null) throw new AiValidationException("预设模型不存在。");
            if (previous is null && presets.Count >= MaxPresetModels)
                throw new AiValidationException($"最多保存 {MaxPresetModels} 个预设模型。");

            var saved = ValidateModel(input, previous);
            if (previous is null) presets.Add(saved);
            else presets[presets.IndexOf(previous)] = saved;
            WritePresetsUnlocked(presets);
        }
        finally
        {
            _presetLock.Release();
        }
    }

    public async Task DeletePresetAsync(string id, CancellationToken cancellationToken)
    {
        if (!ValidId(id)) throw new AiValidationException("模型 ID 无效。");
        await _presetLock.WaitAsync(cancellationToken);
        try
        {
            var presets = ReadPresetsUnlocked();
            if (presets.RemoveAll(item => item.Id == id) == 0)
                throw new AiValidationException("预设模型不存在。");
            WritePresetsUnlocked(presets);
        }
        finally
        {
            _presetLock.Release();
        }
    }

    public Task SavePreferencesAsync(
        string userId,
        ChatPreferences preferences,
        CancellationToken cancellationToken)
    {
        return WithUserLockAsync(userId, async data =>
        {
            data.Preferences = new ChatPreferences { SendOnEnter = preferences.SendOnEnter };
            await WriteUserUnlockedAsync(userId, data, cancellationToken);
        }, cancellationToken);
    }

    public async Task<List<ConversationSummary>> ListConversationsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var data = await ReadUserAsync(userId, cancellationToken);
        return data.Conversations
            .OrderByDescending(item => item.UpdatedAt)
            .Select(ToSummary)
            .ToList();
    }

    public async Task<ConversationRecord?> GetConversationAsync(
        string userId,
        string id,
        CancellationToken cancellationToken)
    {
        if (!ValidConversationId(id)) return null;
        var data = await ReadUserAsync(userId, cancellationToken);
        return data.Conversations.FirstOrDefault(item => item.Id == id);
    }

    public Task SaveConversationAsync(
        string userId,
        ConversationRecord conversation,
        CancellationToken cancellationToken)
    {
        return WithUserLockAsync(userId, async data =>
        {
            data.Conversations.RemoveAll(item => item.Id == conversation.Id);
            data.Conversations.Add(conversation);
            data.Conversations = data.Conversations
                .OrderByDescending(item => item.UpdatedAt)
                .Take(MaxConversations)
                .ToList();
            await WriteUserUnlockedAsync(userId, data, cancellationToken);
        }, cancellationToken);
    }

    public async Task<int> DeleteConversationsAsync(
        string userId,
        IEnumerable<string> ids,
        CancellationToken cancellationToken)
    {
        var requested = ids.Where(ValidConversationId).Distinct().Take(MaxConversations).ToHashSet();
        var removed = 0;
        await WithUserLockAsync(userId, async data =>
        {
            removed = data.Conversations.RemoveAll(item => requested.Contains(item.Id));
            if (removed > 0) await WriteUserUnlockedAsync(userId, data, cancellationToken);
        }, cancellationToken);
        return removed;
    }

    private async Task<List<SavedModel>> ReadPresetsAsync(CancellationToken cancellationToken)
    {
        await _presetLock.WaitAsync(cancellationToken);
        try
        {
            return ReadPresetsUnlocked();
        }
        finally
        {
            _presetLock.Release();
        }
    }

    private static List<SavedModel> ReadPresetsUnlocked()
    {
        var token = SDK.MSLX.Config.GetPluginConfig(MSLXPluginEntry.PluginId).ReadConfigKey("presets");
        if (token is not JArray array) return [];
        try
        {
            return array.ToObject<List<SavedModel>>()?
                       .Where(IsStoredModelValid)
                       .Take(MaxPresetModels)
                       .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void WritePresetsUnlocked(List<SavedModel> presets)
    {
        SDK.MSLX.Config.GetPluginConfig(MSLXPluginEntry.PluginId)
            .WriteConfigKey("presets", JArray.FromObject(presets));
    }

    private async Task<UserData> ReadUserAsync(string userId, CancellationToken cancellationToken)
    {
        var gate = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadUserUnlockedAsync(userId, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task WithUserLockAsync(
        string userId,
        Func<UserData, Task> action,
        CancellationToken cancellationToken)
    {
        var gate = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await action(await ReadUserUnlockedAsync(userId, cancellationToken));
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<UserData> ReadUserUnlockedAsync(string userId, CancellationToken cancellationToken)
    {
        var path = UserFile(userId);
        if (!File.Exists(path)) return new UserData();
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var data = await JsonSerializer.DeserializeAsync<UserData>(stream, JsonOptions, cancellationToken)
                       ?? new UserData();
            data.Models = data.Models.Where(IsStoredModelValid).Take(MaxPersonalModels).ToList();
            data.Conversations = data.Conversations
                .Where(item => item.Owner == userId && ValidConversationId(item.Id))
                .OrderByDescending(item => item.UpdatedAt)
                .Take(MaxConversations)
                .ToList();
            return data;
        }
        catch (JsonException)
        {
            return new UserData();
        }
    }

    private async Task WriteUserUnlockedAsync(
        string userId,
        UserData data,
        CancellationToken cancellationToken)
    {
        var path = UserFile(userId);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
        {
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temp, path, true);
    }

    private string UserFile(string userId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId))).ToLowerInvariant();
        return Path.Combine(DataPath, "users", hash + ".json");
    }

    private static ModelOption ToOption(SavedModel model, string source, bool includeEndpoint = false)
    {
        return new ModelOption
        {
            Id = $"{source}:{model.Id}",
            Name = model.Name,
            Model = model.Model,
            Source = source,
            Endpoint = source == "personal" || includeEndpoint ? model.Endpoint : null,
            HasApiKey = source == "personal" || includeEndpoint ? !string.IsNullOrEmpty(model.ApiKey) : null,
            ThinkingEnabled = model.ThinkingEnabled,
            ThinkingEffort = model.ThinkingEffort
        };
    }

    private static ConversationSummary ToSummary(ConversationRecord conversation)
    {
        return new ConversationSummary
        {
            Id = conversation.Id,
            Title = conversation.Title,
            ModelId = conversation.ModelId,
            ModelName = conversation.ModelName,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static SavedModel ValidateModel(ModelInput input, SavedModel? previous)
    {
        var name = input.Name.Trim();
        var endpoint = input.Endpoint.Trim();
        var model = input.Model.Trim();
        var key = input.ApiKey.Trim();
        if (name.Length is < 1 or > 100 || model.Length is < 1 or > 200)
            throw new AiValidationException("模型名称或模型标识无效。");
        if (key.Length > 4096 || key.Contains('\r') || key.Contains('\n'))
            throw new AiValidationException("API Key 无效。");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || endpoint.Length > 2048)
            throw new AiValidationException("模型接口地址无效。");
        if (input.ThinkingEffort is not ("low" or "medium" or "high"))
            throw new AiValidationException("思考强度无效。");

        var id = input.Id ?? Guid.NewGuid().ToString("N")[..24];
        if (!ValidId(id)) throw new AiValidationException("模型 ID 无效。");
        return new SavedModel
        {
            Id = id,
            Name = name,
            Endpoint = endpoint,
            Model = model,
            ApiKey = input.ClearApiKey ? string.Empty : key.Length > 0 ? key : previous?.Endpoint == endpoint ? previous.ApiKey : string.Empty,
            ThinkingEnabled = input.ThinkingEnabled,
            ThinkingEffort = input.ThinkingEffort
        };
    }

    private static bool IsStoredModelValid(SavedModel model)
    {
        return ValidId(model.Id) && !string.IsNullOrWhiteSpace(model.Name) &&
               !string.IsNullOrWhiteSpace(model.Endpoint) && !string.IsNullOrWhiteSpace(model.Model);
    }

    private static bool ValidId(string id) =>
        id.Length is >= 1 and <= 64 && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private static bool ValidConversationId(string id) =>
        id.Length == 32 && id.All(character => char.IsAsciiHexDigit(character));
}

public sealed class AiValidationException(string message) : Exception(message);
