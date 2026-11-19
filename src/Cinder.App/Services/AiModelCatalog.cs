using System.Net.Http.Headers;
using System.Text.Json;

namespace Cinder.App.Services;

/// <summary>
/// Fetches the list of model ids an AI endpoint serves, so the examiner picks one from a
/// dropdown instead of typing a name blind. Request-only-on-click: like the rest of Cinder,
/// this never talks to the network unless the examiner asks it to.
/// </summary>
public static class AiModelCatalog
{
    /// <summary>Default endpoint per provider, used when the address box is empty.</summary>
    public static string DefaultEndpoint(string providerId) => providerId switch
    {
        "ollama" => "http://localhost:11434",
        "lm-studio" => "http://localhost:1234",
        "openai" => "https://api.openai.com/v1",
        "openai-compatible" => "http://localhost:8080/v1",
        _ => "",
    };

    /// <summary>
    /// The URLs to try, in order, for one provider: OpenAI-compatible builds read
    /// <c>GET {base}/models</c> (adding <c>/v1</c> when the address does not already carry it);
    /// Ollama reads its native <c>GET {base}/api/tags</c> first, then the OpenAI shape.
    /// </summary>
    public static IReadOnlyList<string> BuildCandidateUrls(string providerId, string? endpoint)
    {
        if (!TryNormalizeBase(endpoint, out var baseUri, out _) || baseUri is null)
        {
            return Array.Empty<string>();
        }
        var baseUrl = baseUri.AbsoluteUri.TrimEnd('/');
        if (providerId == "ollama")
        {
            return baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? [baseUrl + "/models", baseUrl[..^3] + "/api/tags"]
                : [baseUrl + "/api/tags", baseUrl + "/v1/models"];
        }
        return baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? [baseUrl + "/models"]
            : [baseUrl + "/v1/models", baseUrl + "/models"];
    }

    /// <summary>
    /// Turns a free-typed address into a base URL: a missing scheme gets <c>http://</c> (people
    /// type <c>192.168.1.5:11434</c>), a bare host gets the default port folded in by Uri, and
    /// a non-absolute string is refused with the reason.
    /// </summary>
    public static bool TryNormalizeBase(string? endpoint, out Uri? uri, out string? problem)
    {
        uri = null;
        problem = null;
        var text = endpoint?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            problem = "地址为空。";
            return false;
        }
        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "http://" + text;
        }
        try
        {
            uri = new Uri(text);
            if (uri.Scheme is not "http" and not "https")
            {
                problem = $"不支持的协议 {uri.Scheme}：请使用 http:// 或 https:// 地址。";
                return false;
            }
            return true;
        }
        catch (UriFormatException)
        {
            problem = "地址无法解析，示例：http://localhost:11434";
            return false;
        }
    }

    /// <summary>
    /// Fetches the model list. Returns the ids sorted; a failure throws — the caller decides how
    /// to show it, because the address box is the usual culprit.
    /// </summary>
    public static async Task<IReadOnlyList<string>> FetchAsync(
        string providerId, string? endpoint, string? apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrEmpty(DefaultEndpoint(providerId)))
        {
            endpoint = DefaultEndpoint(providerId);
        }
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        foreach (var url in BuildCandidateUrls(providerId, endpoint))
        {
            ct.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            }
            try
            {
                using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    continue;   // try the next URL shape; a 404 from /v1/models on Ollama is normal
                }
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var models = ParseModelList(body);
                if (models.Count > 0)
                {
                    return models;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // Try the next candidate URL; the last failure surfaces to the caller.
            }
        }
        throw new HttpRequestException("无法获取模型列表：请确认地址与服务是否可达（Ollama 默认 http://localhost:11434，LM Studio 默认 http://localhost:1234）。");
    }

    /// <summary>
    /// Reads model ids out of the two response shapes in the wild:
    /// OpenAI <c>{"data":[{"id":"llama3"}]}</c> and Ollama <c>{"models":[{"name":"llama3"}]}</c>.
    /// Anything else (an error object, an array of strings, plain text) yields what it can: an
    /// array of strings is accepted, an object is not.
    /// </summary>
    public static IReadOnlyList<string> ParseModelList(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var ids = new List<string>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                CollectStringArray(root, ids);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var id = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var idProp)
                            ? idProp.GetString()
                            : item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                        AddId(ids, id);
                    }
                }
                else if (root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in models.EnumerateArray())
                    {
                        var name = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString()
                            : item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                        AddId(ids, name);
                    }
                }
            }
            return ids.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void CollectStringArray(JsonElement array, List<string> ids)
    {
        foreach (var item in array.EnumerateArray())
        {
            AddId(ids, item.ValueKind == JsonValueKind.String ? item.GetString() : null);
        }
    }

    private static void AddId(List<string> ids, string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ids.Add(id.Trim());
        }
    }
}
