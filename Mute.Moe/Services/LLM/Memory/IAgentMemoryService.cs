namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Provides memory for LLM agents
/// </summary>
public interface IAgentMemoryService
{
    /// <summary>
    /// Create a new memory
    /// </summary>
    public void ProposeMemory(ulong context, string claim, Subject subject, Category category, string evidence, float confidence);

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

    public enum Category
    {
        UserPreference,
        UserTrait,

        InteractionPattern,

        Commitment,

        SelfPreference,
        SelfTrait,

        NarrativeEvent
    }

    public enum Lifetime
    {
        Ephemeral,
        Permanent,
    }
}