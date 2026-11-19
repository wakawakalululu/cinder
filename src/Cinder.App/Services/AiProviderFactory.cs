using Cinder.AI;

namespace Cinder.App.Services;

/// <summary>
/// Builds an <see cref="IAiProvider"/> from the settings-page fields (provider id, address,
/// model name, API key). Shared by the AI Copilot tool page and the docked AI panel, so both
/// speak to the same endpoint the same way and refuse with the same sentences.
/// </summary>
public static class AiProviderFactory
{
    /// <summary>
    /// Builds the provider, or explains why it cannot. Never throws: the endpoint is free text
    /// and people type <c>192.168.1.5:11434</c> without a scheme. For "disabled" this returns
    /// true with a <see cref="DisabledProvider"/> — the caller decides how to present that.
    /// </summary>
    public static bool TryBuild(
        string providerId, string? endpoint, string? model, string? apiKey,
        HttpClient http, out IAiProvider provider, out string? problem)
    {
        provider = new DisabledProvider();
        problem = null;

        if (providerId is not ("ollama" or "lm-studio" or "openai-compatible" or "openai"))
        {
            return true;   // "disabled": the provider itself reports that it is switched off
        }
        if (!AiModelCatalog.TryNormalizeBase(endpoint, out var uri, out problem) || uri is null)
        {
            problem = $"地址无效：{problem}";
            return false;
        }
        if (string.IsNullOrWhiteSpace(model))
        {
            problem = "请先填写模型名称：可点“获取模型列表”从上游拉取，或手填（例如 llama3）。";
            return false;
        }
        provider = providerId switch
        {
            "ollama" => new OllamaProvider(http, model, uri),
            "lm-studio" => new LmStudioProvider(http, model, uri),
            _ => new OpenAiCompatibleProvider(http, providerId, providerId, uri, model, () => apiKey),
        };
        return true;
    }
}
