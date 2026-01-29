namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// One way link between memories
/// </summary>
public class AgentMemoryLink
{
    /// <summary>
    /// Unique ID of this link
    /// </summary>
    public int ID { get; init; }

    /// <summary>
    /// ID of the source memory
    /// </summary>
    public int MemorySrc { get; init; }

    /// <summary>
    /// ID of the destination memory
    /// </summary>
    public int MemoryDst { get; init; }

    /// <summary>
    /// Type of link
    /// </summary>
    public LinkType Type { get; init; }
}

/// <summary>
/// The type of link between memories
/// </summary>
public enum LinkType
{
    /// <summary>
    /// Src and Dst memories contradict each other
    /// </summary>
    Contradiction,

    /// <summary>
    /// Src memory is the cause of Dst memory
    /// </summary>
    Causation,

    /// <summary>
    /// Memories are related by topic
    /// </summary>
    Topic,
}