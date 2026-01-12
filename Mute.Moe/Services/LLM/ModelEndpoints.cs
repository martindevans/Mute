using LlmTornado;
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

public record LlmChatModel(ChatModel Model);
public record LlmEmbeddingModel(EmbeddingModel Model);
public record LlmVisionModel(ChatModel Model);
public record LlmRerankModel(RerankModel Model);