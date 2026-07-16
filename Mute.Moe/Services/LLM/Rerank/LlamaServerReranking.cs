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

namespace Mute.Moe.Services.LLM.Rerank;

/// <summary>
/// Reranking using llama-server API
/// </summary>
public sealed class LlamaServerReranking
    : IReranking
{
    private readonly MultiBackendServiceProvider<LLamaServerEndpoint> _endpoints;
    private readonly HttpClient _http;
    private readonly string _model;

    /// <summary>
    /// Create a new reranker
    /// </summary>
    /// <param name="http"></param>
    /// <param name="model"></param>
    /// <param name="endpoints"></param>
    public LlamaServerReranking(IHttpClientFactory http, AgentRerankModel model, MultiBackendServiceProvider<LLamaServerEndpoint> endpoints)
    {
        _endpoints = endpoints;
        _http = http.CreateClient();

        _model = model.Name;
    }

    /// <inheritdoc />
    public async Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents, CancellationToken cancellation = default)
    {
        // Get an endpoint
        using var endpoint = await _endpoints.Acquire([ _model ], cancellation);
        if (endpoint == null)
            return await new NullRerank().Rerank(query, documents, cancellation);

        // Create content
        var json = JsonSerializer.Serialize(new RerankRequest
        (
            _model,
            query,
            documents.Count,
            documents
        ));
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Bearer", endpoint.Backend.Value.Key);

        // Create request
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new(endpoint.Backend.Value.Url), "/v1/rerank"));
        request.Content = content;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Backend.Value.Key);

        // Send request
        using var res = await _http.SendAsync(request, cancellation);
        res.EnsureSuccessStatusCode();

        // Read response
        await using var stream = await res.Content.ReadAsStreamAsync(cancellation);
        var result = await JsonSerializer.DeserializeAsync<RerankResponse>(stream, cancellationToken: cancellation)
                  ?? throw new InvalidDataException("Invalid rerank response");

        // Convert to result type, sorted highest relevance first
        var results = (
            from item in result.Results
            orderby item.RelevanceScore descending 
            select new RerankResult(documents[item.Index], item.Index, item.RelevanceScore)
        ).ToList();

        return results;
    }

    #region JSON model
    private sealed record RerankRequest(
        [property: JsonPropertyName("model"), UsedImplicitly] string Model,
        [property: JsonPropertyName("query"), UsedImplicitly] string Query,
        [property: JsonPropertyName("top_n"), UsedImplicitly] int Count,
        [property: JsonPropertyName("documents"), UsedImplicitly] IReadOnlyList<string> Documents
        
    );
    
    [UsedImplicitly]
    private sealed record RerankResponse(
        [property: JsonPropertyName("results")] RerankItem[] Results
    );
    
    [UsedImplicitly]
    private sealed record RerankItem(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("relevance_score")] float RelevanceScore
    );
    #endregion
}