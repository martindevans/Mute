using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Embedding;

/// <summary>
/// Provides functions to embed text
/// </summary>
public interface IEmbeddings
{
    /// <summary>
    /// Embed a single item
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    Task<EmbeddingResult?> Embed(string text);

    /// <summary>
    /// Embed many items in one request
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    Task<IReadOnlyList<EmbeddingResult>?> Embed(params string[] text);

    /// <summary>
    /// The name of model used for embeddings generation
    /// </summary>
    string Model { get; }

    /// <summary>
    /// The dimensionality of embeddings
    /// </summary>
    int Dimensions { get; }
}

/// <summary>
/// Result of an embedding operation
/// </summary>
/// <param name="Input">String that was embedded</param>
/// <param name="Model">Model name</param>
/// <param name="Result">The actual embedding</param>
public record EmbeddingResult(string Input, string Model, Memory<float> Result);