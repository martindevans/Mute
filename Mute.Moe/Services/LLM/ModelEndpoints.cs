using HandyAgentFramework.Models;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// A llama-server API endpoint
/// </summary>
/// <param name="ID">Unique ID for this backend (may be displayed in the UI)</param>
/// <param name="Url">Endpoint URL</param>
/// <param name="Key">Secret API key</param>
/// <param name="ModelsBlacklist">Models that should not be accessed through this endpoint</param>
public record LLamaServerEndpoint(string ID, string Url, string Key, IReadOnlySet<string> ModelsBlacklist);

/// <summary>
/// LLM to use for chat based features
/// </summary>
[UsedImplicitly]
public record AgentChatModel(string Name, int ContextSize, bool IsVisionModel) : IChatModel;

/// <summary>
/// Model to use for generating summaries
/// </summary>
[UsedImplicitly]
public record AgentSummaryModel(string Name, int ContextSize) : IChatModel;

/// <summary>
/// Model to use for generating embeddings
/// </summary>
[UsedImplicitly]
public record AgentEmbeddingModel(string Name, int ContextSize, int EmbeddingDimensions) : IEmbeddingModel;

/// <summary>
/// VLM to use for vision based features
/// </summary>
[UsedImplicitly]
public record AgentVisionModel(string Name, int ContextSize) : IChatModel;

/// <summary>
/// Model to use for query reranking
/// </summary>
[UsedImplicitly]
public record AgentRerankModel(string Name, int ContextSize) : IRerankingModel;