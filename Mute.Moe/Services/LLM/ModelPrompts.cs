namespace Mute.Moe.Services.LLM;

/// <summary>
/// System prompt for LLM chat
/// </summary>
/// <param name="Prompt"></param>
public record ChatConversationSystemPrompt(string Prompt);

/// <summary>
/// System prompt for fact extraction
/// </summary>
/// <param name="Prompt"></param>
public record AgentFactExtractionSystemPrompt(string Prompt);
