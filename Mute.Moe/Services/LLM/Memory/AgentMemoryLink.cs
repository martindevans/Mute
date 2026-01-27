namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// One way link between memories
/// </summary>
public class AgentMemoryLink
{
    public long ID { get; init; }

    public long MemorySrc { get; init; }
    public long MemoryDst { get; init; }

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