namespace Mute.Moe.Services.LLM;

/// <summary>
/// A system prompt for an LLM
/// </summary>
public interface ILlmPrompt
{
    /// <summary>
    /// The prompt
    /// </summary>
    string Prompt { get; }
}

/// <summary>
/// System prompt for LLM chat
/// </summary>
/// <param name="Prompt"></param>
public record ChatConversationSystemPrompt(string Prompt) : ILlmPrompt;

/// <summary>
/// System prompt for fact extraction
/// </summary>
/// <param name="Prompt"></param>
public record AgentFactExtractionSystemPrompt(string Prompt) : ILlmPrompt;
