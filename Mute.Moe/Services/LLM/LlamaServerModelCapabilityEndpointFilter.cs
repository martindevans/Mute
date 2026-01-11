using FluidCaching;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// Get the models list from llama-server and filters out servers which do not include the filter strings in the model list
/// </summary>
public class LlamaServerModelCapabilityEndpointFilter
    : MultiEndpointProvider<LLamaServerEndpoint>.IEndpointFilter
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly FluidCache<CacheItem> _modelsCache;
    private readonly IIndex<string, CacheItem> _modelsByBackendId;

    private readonly HttpClient _http;

    /// <summary>
    /// 
    /// </summary>
    public LlamaServerModelCapabilityEndpointFilter(IHttpClientFactory http)
    {
        _modelsCache = new FluidCache<CacheItem>(8, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(15), () => DateTime.UtcNow);
        _modelsByBackendId = _modelsCache.AddIndex("IndexByUniqueId", a => a.Id);

        _http = http.CreateClient();
    }

    /// <inheritdoc />
    public async ValueTask<bool> FilterEndpoint(LLamaServerEndpoint endpoint, string[] filters)
    {
        var models = await _modelsByBackendId.GetItem(endpoint.ID, _ => GetBackendModelList(endpoint));

        foreach (var filter in filters)
            if (!models.Models.Contains(filter))
                return false;

        return true;
    }

    private async Task<CacheItem> GetBackendModelList(LLamaServerEndpoint endpoint)
    {
        var models = await _http.GetFromJsonAsync<ModelsList>(new Uri(new(endpoint.Url), "models"));
        if (models == null)
            return new CacheItem(endpoint.ID, [ ]);

        return new CacheItem(
            endpoint.ID,
            models.Models.Select(a => a.ID).ToHashSet()
        );
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
        [JsonPropertyName("id")]
        public required string ID { get; init; }
    }
}