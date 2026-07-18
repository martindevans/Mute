using HandyAgentFramework.Prompts;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// System prompt for LLM chat
/// </summary>
/// <param name="Prompt"></param>
public record ChatConversationSystemPrompt(string Prompt)
    : ISystemPrompt;
