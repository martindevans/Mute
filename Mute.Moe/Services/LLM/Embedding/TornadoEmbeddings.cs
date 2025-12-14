using System.Threading.Tasks;
using Serilog;

namespace Mute.Moe.Services.LLM.Embedding;

/// <summary>
/// Provides embeddings using LlmTornado
/// </summary>
public class TornadoEmbeddings
    : IEmbeddings
{
    private readonly EmbeddingModelEndpoint _embedding;

    /// <inheritdoc />
    public string Model => _embedding.Model.Name;

    /// <inheritdoc />
    public int Dimensions => _embedding.Model.OutputDimensions ?? 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="embedding"></param>
    public TornadoEmbeddings(EmbeddingModelEndpoint embedding)
    {
        _embedding = embedding;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult?> Embed(string text)
    {
        try
        {
            var embedding = await _embedding.Api.Embeddings.CreateEmbedding(_embedding.Model, text, Dimensions);

            if (embedding == null || embedding.Data.Count < 1)
                return null;

            return new EmbeddingResult(text, _embedding.Model.Name, embedding.Data[0].Embedding);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Embedding error");
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingResult>?> Embed(params string[] text)
    {
        var embeddings = await _embedding.Api.Embeddings.CreateEmbedding(_embedding.Model, text);

        if (embeddings == null)
            return null;

        var results = new List<EmbeddingResult>();
        foreach (var embedding in embeddings.Data)
            results.Add(new EmbeddingResult(text[embedding.Index], _embedding.Model.Name, embedding.Embedding));
        return results;
    }
}