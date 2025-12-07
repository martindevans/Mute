using LlmTornado;
using LlmTornado.Embedding.Models;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

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
        var embedding = await _api.Embeddings.CreateEmbedding(_model, text, Dimensions);

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