using LlmTornado.Embedding.Models;
using System.IO;
using System.Net.Http;
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
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;
    private readonly HttpClient _http;
    private readonly EmbeddingModel _model;

    /// <inheritdoc />
    public string Model => _model.Name;

    /// <inheritdoc />
    public int Dimensions => _model.OutputDimensions ?? 0;

    /// <summary>
    /// Create a new reranker
    /// </summary>
    /// <param name="http"></param>
    /// <param name="model"></param>
    /// <param name="endpoints"></param>
    public LLamaServerEmbedding(IHttpClientFactory http, LlmEmbeddingModel model, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    {
        _endpoints = endpoints;
        _http = http.CreateClient();

        _model = model.Model;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult?> Embed(string text, CancellationToken cancellation = default)
    {
        var results = await Embed([ text ], cancellation);
        return results?[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingResult>?> Embed(string[] text, CancellationToken cancellation = default)
    {
        // Get an endpoint
        using var endpoint = await _endpoints.GetEndpoint(cancellation);
        if (endpoint == null)
            return null;

        // Create request
        var json = JsonSerializer.Serialize(new
        {
            input = text,
            model = Model,
            encoding_format = "float"
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Bearer", endpoint.Endpoint.Key);

        // Send request
        using var res = await _http.PostAsync(
            new Uri(new(endpoint.Endpoint.Url), "/v1/embeddings"),
            content,
            cancellation
        );
        res.EnsureSuccessStatusCode();

        // Read response
        await using var stream = await res.Content.ReadAsStreamAsync(cancellation);
        var result = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(stream, cancellationToken: cancellation)
                  ?? throw new InvalidDataException("Invalid embedding response");

        var results = result.Data.Select(a => new EmbeddingResult(text[a.Index], result.Model, a.Embedding)).ToArray();
        return results;
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public required EmbeddingItem[] Data { get; set; }

        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("usage")]
        public required EmbeddingUsage Usage { get; set; }
    }

    private class EmbeddingItem
    {
        [JsonPropertyName("embedding")]
        public required float[] Embedding { get; set; }

        [JsonPropertyName("index")]
        public required int Index { get; set; }
    }

    private class EmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}