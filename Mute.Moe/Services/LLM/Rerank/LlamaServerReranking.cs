using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Rerank;

/// <summary>
/// Reranking using llama-server API
/// </summary>
public sealed class LlamaServerReranking
    : IReranking
{
    private readonly HttpClient _http;
    private readonly string _model;

    /// <summary>
    /// Create a new reranker
    /// </summary>
    /// <param name="http"></param>
    /// <param name="model"></param>
    public LlamaServerReranking(IHttpClientFactory http, RerankModelEndpoint model)
    {
        _http = http.CreateClient();
        _http.BaseAddress = new Uri(model.BaseUrl, UriKind.Absolute);

        _model = model.Model;
    }

    /// <inheritdoc />
    public async Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents)
    {
        // Create request
        var json = JsonSerializer.Serialize(new
        {
            model = _model,
            query,
            top_n = documents.Count,
            documents
        });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send request
        using var res = await _http.PostAsync("/v1/rerank", content);
        res.EnsureSuccessStatusCode();

        // Read response
        await using var stream = await res.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<RerankResponse>(stream)
                  ?? throw new InvalidDataException("Invalid rerank response");

        // Convert to result type
        var results = new List<RerankResult>(result.results.Length);
        foreach (var r in result.results)
            results.Add(new RerankResult(r.index, r.relevance_score));

        // Sort into order, highest relevance first
        return results.OrderByDescending(a => a.Relevance).ToList();
    }

    private sealed class RerankResponse
    {
        public RerankItem[] results { get; set; } = [ ];
    }

    private sealed class RerankItem
    {
        public int index { get; set; }
        public float relevance_score { get; set; }
    }
}