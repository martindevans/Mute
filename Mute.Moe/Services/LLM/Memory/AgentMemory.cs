using Dapper.Contrib.Extensions;

namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// A fact triple stored by the agent
/// </summary>
public class AgentMemory
{
    /// <summary>
    /// The unique ID of this memory
    /// </summary>
    public int ID { get; init; }

    /// <summary>
    /// The context in which this memory may be used
    /// </summary>
    public ulong Context { get; init; }

    /// <summary>
    /// The text of this memory, the actual data
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The embedding of the Text field
    /// </summary>
    public byte[] Embedding { get; init; } = [];

    /// <summary>
    /// The model used to generate the embedding
    /// </summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>
    /// Confidence this memory is true/correct
    /// </summary>
    public float ConfidenceLogit { get; init; }

    /// <summary>
    /// Creation time of this memory (unix timestamp)
    /// </summary>
    public ulong CreationUnix { get; init; }

    /// <summary>
    /// Creation time of this memory
    /// </summary>
    [Computed] public DateTime Creation => CreationUnix.FromUnixTimestamp();

    /// <summary>
    /// Access time of this memory (unix timestamp)
    /// </summary>
    public ulong AccessUnix { get; init; }

    /// <summary>
    /// Access time of this memory
    /// </summary>
    [Computed] public DateTime Access => AccessUnix.FromUnixTimestamp();
}