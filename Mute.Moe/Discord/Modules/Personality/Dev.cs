using Discord.Commands;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.LLM;
using Mute.Moe.Services.LLM.Memory;
using Mute.Moe.Services.LLM.Memory.Extraction;

namespace Mute.Moe.Discord.Modules.Personality;

[UsedImplicitly]
[RequireOwner]
public partial class Dev(IConversationStateStorage convState, IAgentMemoryStorage memory, AgentMemoryConfidenceDecayOverTime decay, FactExtractionService factExtract, LlmFactModel factModel, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    : MuteBaseModule
{
}