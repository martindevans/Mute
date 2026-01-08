using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Embedding;

/// <summary>
/// Provides embeddings using LlmTornado
/// </summary>
public class TornadoEmbeddings
    : IEmbeddings
{
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;
    private readonly LlmEmbeddingModel _model;

    /// <inheritdoc />
    public string Model => _model.Model.Name;

    /// <inheritdoc />
    public int Dimensions => _model.Model.OutputDimensions ?? 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="model"></param>
    public TornadoEmbeddings(MultiEndpointProvider<LLamaServerEndpoint> endpoints, LlmEmbeddingModel model)
    {
        _model = model;
        _endpoints = endpoints;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult?> Embed(string text, CancellationToken cancellation = default)
    {
        using var endpoint = await _endpoints.GetEndpoint(cancellation);
        if (endpoint == null)
            return null;
        var api = endpoint.Endpoint.TornadoApi;

        var embedding = await api.Embeddings.CreateEmbedding(_model.Model, text, Dimensions);
        if (embedding == null || embedding.Data.Count < 1)
            return null;

        return new EmbeddingResult(text, Model, embedding.Data[0].Embedding);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingResult>?> Embed(string[] text, CancellationToken cancellation = default)
    {
        using var endpoint = await _endpoints.GetEndpoint(cancellation);
        if (endpoint == null)
            return null;
        var api = endpoint.Endpoint.TornadoApi;

        var embeddings = await api.Embeddings.CreateEmbedding(_model.Model, text);

        if (embeddings == null)
            return null;

        var results = new List<EmbeddingResult>();
        foreach (var embedding in embeddings.Data)
            results.Add(new EmbeddingResult(text[embedding.Index], Model, embedding.Embedding));
        return results;
    }
}