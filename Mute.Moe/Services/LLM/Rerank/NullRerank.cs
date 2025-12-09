using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Rerank;

/// <summary>
/// Do not actually re-rank results
/// </summary>
public class NullRerank
    : IReranking
{
    /// <inheritdoc />
    public Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents)
    {
        var results = new List<RerankResult>(documents.Count);

        for (var i = 0; i < documents.Count; i++)
        {
            // Rank documents in order, so the first receives the highest relevance. Keep
            // relevance of all documents fairly high.
            var t = i / (float)documents.Count;
            var r = float.Lerp(1, 0.9f, t);

            results.Add(new RerankResult(i, r));
        }

        return Task.FromResult(results);
    }
}