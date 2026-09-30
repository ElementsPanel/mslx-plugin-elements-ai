using Microsoft.AspNetCore.Mvc.ApplicationParts;
using MSLX.Plugin.ElementsAI.Services;
using MSLX.SDK;
using MSLX.SDK.Interfaces;

[assembly: ApplicationPart("MSLX.Plugin.ElementsAI")]

namespace MSLX.Plugin.ElementsAI;

public sealed class MSLXPluginEntry : IPlugin
{
    public const string PluginId = "mslx-plugin-elements-ai";

    public static MSLXPluginEntry Instance { get; private set; } = null!;

    public string Id => PluginId;
    public string Name => "Elements AI";
    public string Description => "让 Agent 接管面板。";
    public string Version => "0.1.22";
    public string Icon => "icon.png";
    public string MinSDKVersion => "1.7.0.2";
    public string Developer => "JessDaodao";
    public string AuthorUrl => "https://github.com/JessDaodao";
    public string PluginUrl => "https://github.com/ElementsPanel/mslx-plugin-elements-ai";

    public void OnPluginInitialize(IServiceProvider serviceProvider)
    {
        Instance = this;
        Directory.CreateDirectory(this.Config().GetDataPath());
        SDK.MSLX.Logger.Info("[Elements AI] 插件初始化完成。");
    }

    public void OnLoad()
    {
        SDK.MSLX.Logger.Info("[Elements AI] 已载入，可以从全局页眉右侧打开 AI 助手。");
    }

    public void OnUnload()
    {
        SDK.MSLX.Logger.Info("[Elements AI] 已卸载。");
    }

    public void OnRegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    public void OnRegisterServices(IServiceCollection services)
    {
        services.AddHttpClient();
        // MSLX forwards host singleton registrations as factories, which is
        // incompatible with AddHttpClient(name)'s instance-backed registries.
        services.AddSingleton(_ => new NodeCommandService(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        }));
        services.AddSingleton<AiDataStore>();
        services.AddSingleton<TaskProgressService>();
        services.AddSingleton<OpenAiProvider>();
        services.AddSingleton<AiChatService>();
    }
}
