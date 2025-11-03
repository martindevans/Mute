using System.Threading.Tasks;
using LlmTornado;
using LlmTornado.Embedding.Models;

namespace Mute.Moe.Services.LLM;

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
/// <param name="Input"></param>
/// <param name="Model"></param>
/// <param name="Result"></param>
public record EmbeddingResult(string Input, string Model, Memory<float> Result);

/// <summary>
/// Provides embeddings using LlmTornado
/// </summary>
public class TornadoEmbeddings
    : IEmbeddings
{
    private readonly TornadoApi _api;
    private readonly EmbeddingModel _model;

    /// <inheritdoc />
    public string Model => _model.Name;

    /// <inheritdoc />
    public int Dimensions => _model.OutputDimensions ?? 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="api"></param>
    /// <param name="model"></param>
    public TornadoEmbeddings(TornadoApi api, EmbeddingModel model)
    {
        _api = api;
        _model = model;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult?> Embed(string text)
    {
        var embedding = await _api.Embeddings.CreateEmbedding(_model, text);

        if (embedding == null || embedding.Data.Count < 1)
            return null;

        return new EmbeddingResult(text, _model.Name, embedding.Data[0].Embedding);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingResult>?> Embed(params string[] text)
    {
        var embeddings = await _api.Embeddings.CreateEmbedding(_model, text);

        if (embeddings == null)
            return null;

        var results = new List<EmbeddingResult>();
        foreach (var embedding in embeddings.Data)
            results.Add(new EmbeddingResult(text[embedding.Index], _model.Name, embedding.Embedding));
        return results;
    }
}