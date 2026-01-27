using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding.Models;
using LlmTornado.Rerank.Models;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// A llama-server API endpoint
/// </summary>
/// <param name="ID">Unique ID for this backend (may be displayed in the UI)</param>
/// <param name="Url">Endpoint URL</param>
/// <param name="Key">Secret API key</param>
/// <param name="ModelsBlacklist">Models that should not be accessed through this endpoint</param>
public record LLamaServerEndpoint(string ID, string Url, string Key, IReadOnlySet<string> ModelsBlacklist)
{
    /// <summary>
    /// Get a <see cref="TornadoApi"/> wrapping this endpoint
    /// </summary>
    public TornadoApi TornadoApi
    {
        get
        {
            field ??= new TornadoApi(new Uri(Url), Key);
            return field;
        }
    }
}

/// <summary>
/// Sampling parameters to use for a model
/// </summary>
/// <param name="Temperature"></param>
/// <param name="TopP"></param>
/// <param name="PresencePenalty"></param>
/// <param name="FrequencyPenalty"></param>
[UsedImplicitly]
public record SamplingParameters(
    double? Temperature,
    double? TopP,
    double? PresencePenalty,
    double? FrequencyPenalty
)
{
    /// <summary>
    /// Apply these sampling parameters to the chat request
    /// </summary>
    /// <param name="request"></param>
    public void Apply(ChatRequest request)
    {
        request.Temperature = Temperature;
        request.TopP =TopP;
        request.PresencePenalty = PresencePenalty;
        request.FrequencyPenalty = FrequencyPenalty;
    }
}

/// <summary>
/// LLM model definition
/// </summary>
public interface ILlmModel
{
    /// <summary>
    /// The model
    /// </summary>
    ChatModel Model { get; }

    /// <summary>
    /// Optional sampling parameters
    /// </summary>
    SamplingParameters? Sampling { get; }
}

/// <summary>
/// LLM to use for chat based features
/// </summary>
/// <param name="Model"></param>
/// <param name="Sampling"></param>
public record LlmChatModel(ChatModel Model, SamplingParameters? Sampling) : ILlmModel;

/// <summary>
/// LLM to use for fact extraction based features
/// </summary>
/// <param name="Model"></param>
/// <param name="Sampling"></param>
public record LlmFactModel(ChatModel Model, SamplingParameters? Sampling) : ILlmModel;

/// <summary>
/// Model to use for generating embeddings
/// </summary>
/// <param name="Model"></param>
public record LlmEmbeddingModel(EmbeddingModel Model);

/// <summary>
/// VLM to use for vision based features
/// </summary>
/// <param name="Model"></param>
public record LlmVisionModel(ChatModel Model);

/// <summary>
/// Model to use for query reranking
/// </summary>
/// <param name="Model"></param>
public record LlmRerankModel(RerankModel Model);