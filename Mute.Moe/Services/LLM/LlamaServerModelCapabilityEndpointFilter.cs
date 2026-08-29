using FluidCaching;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// Get the models list from llama-server and filters out servers which do not include the filter strings in the model list
/// </summary>
public class LlamaServerModelCapabilityEndpointFilter
    : MultiBackendServiceProvider.IBackendFilter<LLamaServerEndpoint>
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly FluidCache<CacheItem> _modelsCache;
    private readonly IIndex<string, CacheItem> _modelsByBackendId;

    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlamaServerModelCapabilityEndpointFilter"/> class.
    /// </summary>
    /// <param name="http">
    /// The <see cref="IHttpClientFactory"/> used to create <see cref="HttpClient"/> instances for interacting with llama-server endpoints.
    /// </param>
    public LlamaServerModelCapabilityEndpointFilter(IHttpClientFactory http)
    {
        _modelsCache = new FluidCache<CacheItem>(8, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(15), () => DateTime.UtcNow);
        _modelsByBackendId = _modelsCache.AddIndex("IndexByUniqueId", a => a.Id);

        _http = http.CreateClient();
    }

    /// <inheritdoc />
    public async ValueTask<bool> Filter(LLamaServerEndpoint backend, IReadOnlyCollection<string> tags)
    {
        // Check blacklist doesn't ban any of the requested items
        foreach (var filter in tags)
            if (backend.ModelsBlacklist.Contains(filter))
                return false;

        // Get backend models list
        var models = await _modelsByBackendId.GetItem(backend.ID, _ => GetBackendModelList(backend));

        // Check if backend is missing any of the requested models
        foreach (var filter in tags)
            if (!models.Models.Contains(filter))
                return false;

        return true;
    }

    private async Task<CacheItem> GetBackendModelList(LLamaServerEndpoint endpoint)
    {
        var models = await GetFromJsonAsync<ModelsList>(endpoint.Url, endpoint.Key, "models");
        if (models == null)
            return new CacheItem(endpoint.ID, [ ]);

        return new CacheItem(
            endpoint.ID,
            models.Models.Select(a => a.ID).ToHashSet()
        );
    }

    private async Task<T?> GetFromJsonAsync<T>(string url, string key, string path)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(url), path)
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return default;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private record CacheItem(string Id, HashSet<string> Models);

    [UsedImplicitly]
    private class ModelsList
    {
        [JsonPropertyName("data")]
        public required ModelItem[] Models { get; init; }
    }

    [UsedImplicitly]
    private class ModelItem
    {
        [JsonPropertyName("id"), UsedImplicitly]
        public required string ID { get; init; }
    }
}