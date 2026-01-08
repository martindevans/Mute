using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Rerank;

/// <summary>
/// Rerank documents according to a query
/// </summary>
public interface IReranking
{
    /// <summary>
    /// Rank a set of documents for relevance to the given query
    /// </summary>
    /// <param name="query"></param>
    /// <param name="documents"></param>
    /// <param name="cancellation"></param>
    /// <returns>Rerank results, in order of relevance</returns>
    Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents, CancellationToken cancellation = default);
}

/// <summary>
/// Result from reranking a set of documents
/// </summary>
/// <param name="Index"></param>
/// <param name="Relevance"></param>
public readonly record struct RerankResult(int Index, float Relevance)
    : IComparable<RerankResult>
{
    /// <inheritdoc />
    public int CompareTo(RerankResult other)
    {
        return Relevance.CompareTo(other.Relevance);
    }
}