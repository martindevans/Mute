namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Provides memory for LLM agents
/// </summary>
public interface IAgentMemoryService
{
    /// <summary>
    /// Propose creation of a new memory
    /// </summary>
    /// <param name="context">ID for the context this memory is valid in</param>
    /// <param name="claim">The factual claim this memory makes</param>
    /// <param name="subject">Subject of this claim</param>
    /// <param name="evidence">Evidence for this memory, e.g. a quote from a conversation</param>
    /// <param name="confidence">Confidence level of this memory</param>
    public void ProposeMemory(ulong context, string claim, Subject subject, string evidence, Confidence confidence);

    public struct Subject
    {
        public SubjectType Type;
        public ulong  SubjectID1;
        public ulong? SubjectID2;
    }

    public enum SubjectType
    {
        User,
        Self,
        Relationship,
        World
    }

    public enum Confidence
    {
        Low,
        Medium,
        High,
    }
}