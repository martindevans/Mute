using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding.Models;
using LlmTornado.Rerank.Models;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// The chat model and associated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record ChatModelEndpoint(TornadoApi Api, ChatModel Model, bool IsLocal);

/// <summary>
/// The embedding model and asspciated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record EmbeddingModelEndpoint(TornadoApi Api, EmbeddingModel Model, bool IsLocal);

/// <summary>
/// The chat model with vision capabilities and associated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record ImageAnalysisModelEndpoint(TornadoApi Api, ChatModel Model, bool IsLocal);

/// <summary>
/// The reranking model and associated API accessor to use
/// </summary>
/// <param name="Api"></param>
/// <param name="Model"></param>
/// <param name="IsLocal"></param>
/// <param name="IsLocal">Indicates if this model is locally/privately hosted</param>
public record RerankModelEndpoint(TornadoApi Api, RerankModel Model, bool IsLocal);