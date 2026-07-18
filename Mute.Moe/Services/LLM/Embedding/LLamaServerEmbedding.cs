using HandyAgentFramework.Embedding;
using MultiBackendServiceProvider;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Embedding;

/// <summary>
/// Embeds text using llama-server directly
/// </summary>
public class LLamaServerEmbedding
    : IEmbeddings
{
    private readonly MultiBackendServiceProvider<LLamaServerEndpoint> _endpoints;
    private readonly HttpClient _http;
    private readonly AgentEmbeddingModel _model;

    /// <inheritdoc />
    public string Model => _model.Name;

    /// <inheritdoc />
    public int Dimensions => _model.EmbeddingDimensions;

    /// <summary>
    /// Create a new reranker
    /// </summary>
    /// <param name="http"></param>
    /// <param name="model"></param>
    /// <param name="endpoints"></param>
    public LLamaServerEmbedding(IHttpClientFactory http, AgentEmbeddingModel model, MultiBackendServiceProvider<LLamaServerEndpoint> endpoints)
    {
        _endpoints = endpoints;
        _http = http.CreateClient();

        _model = model;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult> Embed(string text, CancellationToken cancellation = default)
    {
        var results = await Embed([ text ], cancellation);
        return results[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingResult>> Embed(IReadOnlyList<string> text, CancellationToken cancellation = default)
    {
        // Get an endpoint
        using var endpoint = await _endpoints.Acquire([ Model ], cancellation);
        if (endpoint == null)
            throw new InvalidOperationException("Cannot retrieve server backend");

        // Create content
        var json = JsonSerializer.Serialize(new
        {
            input = text,
            model = Model,
            encoding_format = "float"
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Create request
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new(endpoint.Backend.Value.Url), "/v1/embeddings"));
        request.Content = content;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Backend.Value.Key);

        // Send request
        using var res = await _http.SendAsync(request, cancellation);
        res.EnsureSuccessStatusCode();

        // Read response
        await using var stream = await res.Content.ReadAsStreamAsync(cancellation);
        var result = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(stream, cancellationToken: cancellation)
                  ?? throw new InvalidDataException("Invalid embedding response");

        var results = result.Data.Select(a => new EmbeddingResult(text[a.Index], result.Model, a.Embedding)).ToArray();
        return results;
    }

    [UsedImplicitly]
    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        [UsedImplicitly]
        public required EmbeddingItem[] Data { get; init; }

        [JsonPropertyName("model")]
        [UsedImplicitly]
        public required string Model { get; init; }
    }

    [UsedImplicitly]
    private class EmbeddingItem
    {
        [JsonPropertyName("embedding")]
        [UsedImplicitly]
        public required float[] Embedding { get; init; }

        [JsonPropertyName("index")]
        [UsedImplicitly]
        public required int Index { get; init; }
    }
}