using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Rerank.Models;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// Base interface for Model+Endpoint records
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public interface IModelEndpoint<out TSelf>
{
    /// <summary>
    /// Create a new instance of this type
    /// </summary>
    /// <param name="api"></param>
    /// <param name="model"></param>
    /// <param name="provider"></param>
    /// <param name="isLocal"></param>
    /// <returns></returns>
    static abstract TSelf Create(TornadoApi api, string model, LLmProviders provider, bool isLocal);
}

/// <summary>
/// The chat model and associated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record ChatModelEndpoint(TornadoApi Api, ChatModel Model, bool IsLocal)
    : IModelEndpoint<ChatModelEndpoint>
{
    /// <inheritdoc />
    public static ChatModelEndpoint Create(TornadoApi api, string model, LLmProviders provider, bool isLocal)
    {
        return new ChatModelEndpoint(api, new ChatModel(model, provider), isLocal);
    }
}

/// <summary>
/// The embedding model and asspciated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record EmbeddingModelEndpoint(TornadoApi Api, EmbeddingModel Model, bool IsLocal)
    : IModelEndpoint<EmbeddingModelEndpoint>
{
    /// <inheritdoc />
    public static EmbeddingModelEndpoint Create(TornadoApi api, string model, LLmProviders provider, bool isLocal)
    {
        return new EmbeddingModelEndpoint(api, new EmbeddingModel(model, provider), isLocal);
    }
}

/// <summary>
/// The chat model with vision capabilities and associated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record ImageAnalysisModelEndpoint(TornadoApi Api, ChatModel Model, bool IsLocal)
    : IModelEndpoint<ImageAnalysisModelEndpoint>
{
    /// <inheritdoc />
    public static ImageAnalysisModelEndpoint Create(TornadoApi api, string model, LLmProviders provider, bool isLocal)
    {
        return new ImageAnalysisModelEndpoint(api, new ChatModel(model, provider), isLocal);
    }
}

/// <summary>
/// The reranking model and associated endpoint to use
/// </summary>
/// <param name="BaseUrl"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record RerankModelEndpoint(string BaseUrl, RerankModel Model, bool IsLocal);