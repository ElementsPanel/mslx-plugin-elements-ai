using System.Reflection;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;

static class ModelEndpointTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("base and legacy model URLs resolve to one chat completion path", () =>
        {
            foreach (var (input, expected) in new[]
            {
                ("https://example.com", "https://example.com/chat/completions"),
                ("https://example.com/v1", "https://example.com/v1/chat/completions"),
                (" https://example.com/v1/// ", "https://example.com/v1/chat/completions"),
                ("https://example.com/proxy/api/v2", "https://example.com/proxy/api/v2/chat/completions"),
                ("http://localhost:8080/v1/chat/completions/", "http://localhost:8080/v1/chat/completions"),
                ("https://example.com/chat/completions", "https://example.com/chat/completions"),
                ("https://example.com/v1/chat/completions/.", "https://example.com/v1/chat/completions"),
                ("https://chat/completions", "https://chat/completions/chat/completions")
            }) Check(ModelEndpoint.ChatCompletions(input) == expected, "endpoint joined incorrectly");
            return Task.CompletedTask;
        });
        yield return ("model URL validation still rejects credentials, queries and invalid schemes", () =>
        {
            foreach (var input in new[] { "", "example.com/v1", "file:///tmp/model", "https://user:secret@example.com/v1", "https://example.com/v1?key=test", "https://example.com/v1#fragment" })
            {
                try { ModelEndpoint.NormalizeBase(input); }
                catch (AiValidationException) { continue; }
                throw new Exception("invalid model endpoint accepted");
            }
            return Task.CompletedTask;
        });
        yield return ("saving legacy endpoints preserves credentials for the same base", () =>
        {
            var previous = new SavedModel { Endpoint = "https://example.com/v1/chat/completions", ApiKey = "test-key" };
            var input = new ModelInput { Id = previous.Id, Name = "Model", Model = "model", Endpoint = "https://example.com/v1/" };
            var method = typeof(AiDataStore).GetMethod("ValidateModel", BindingFlags.Static | BindingFlags.NonPublic)!;
            SavedModel Save() => (SavedModel)method.Invoke(null, [input, previous])!;
            var saved = Save();
            Check(saved.Endpoint == "https://example.com/v1" && saved.ApiKey == "test-key", "normalization discarded existing key");
            input.Endpoint = previous.Endpoint;
            Check(Save().Endpoint == "https://example.com/v1", "saved configuration still contains completion path");
            input.ClearApiKey = true;
            Check(Save().ApiKey == "", "explicit key removal ignored");
            input.ClearApiKey = false;
            input.Endpoint = "https://other.example.com/v1";
            Check(Save().ApiKey == "", "key carried to a different endpoint");
            return Task.CompletedTask;
        });
        yield return ("model options display base URLs while preserving preset privacy", () =>
        {
            var model = new SavedModel { Endpoint = "https://example.com/v1/chat/completions" };
            var method = typeof(AiDataStore).GetMethod("ToOption", BindingFlags.Static | BindingFlags.NonPublic)!;
            ModelOption Option(string source, bool include) => (ModelOption)method.Invoke(null, [model, source, include])!;
            Check(Option("personal", false).Endpoint == "https://example.com/v1", "personal endpoint not normalized");
            Check(Option("preset", true).Endpoint == "https://example.com/v1", "admin endpoint not normalized");
            Check(Option("preset", false).Endpoint is null, "preset endpoint exposed");
            return Task.CompletedTask;
        });
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
