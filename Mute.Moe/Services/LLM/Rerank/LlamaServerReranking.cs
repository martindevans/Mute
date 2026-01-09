using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Rerank;

/// <summary>
/// Reranking using llama-server API
/// </summary>
public sealed class LlamaServerReranking
    : IReranking
{
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;
    private readonly HttpClient _http;
    private readonly string _model;

    /// <summary>
    /// Create a new reranker
    /// </summary>
    /// <param name="http"></param>
    /// <param name="model"></param>
    /// <param name="endpoints"></param>
    public LlamaServerReranking(IHttpClientFactory http, LlmRerankModel model, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    {
        _endpoints = endpoints;
        _http = http.CreateClient();

        _model = model.Model;
    }

    /// <inheritdoc />
    public async Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents, CancellationToken cancellation = default)
    {
        // Get an endpoint
        using var endpoint = await _endpoints.GetEndpoint(cancellation);
        if (endpoint == null)
            return await new NullRerank().Rerank(query, documents, cancellation);

        // Create request
        var json = JsonSerializer.Serialize(new
        {
            model = _model,
            query,
            top_n = documents.Count,
            documents
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Bearer", endpoint.Endpoint.Key);

        // Send request
        using var res = await _http.PostAsync(
            new Uri(new(endpoint.Endpoint.Url), "/v1/rerank"),
            content,
            cancellation
        );
        res.EnsureSuccessStatusCode();

        // Read response
        await using var stream = await res.Content.ReadAsStreamAsync(cancellation);
        var result = await JsonSerializer.DeserializeAsync<RerankResponse>(stream, cancellationToken: cancellation)
                  ?? throw new InvalidDataException("Invalid rerank response");

        // Convert to result type
        var results = new List<RerankResult>(result.Results.Length);
        foreach (var r in result.Results)
            results.Add(new RerankResult(r.Index, r.RelevanceScore));

        // Sort into order, highest relevance first
        return results.OrderByDescending(a => a.Relevance).ToList();
    }

    private sealed class RerankResponse
    {
        [JsonPropertyName("results")]
        public RerankItem[] Results { get; set; } = [ ];
    }

    private sealed class RerankItem
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("relevance_score")]
        public float RelevanceScore { get; set; }
    }
}