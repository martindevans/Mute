using Discord.Commands;
using LlmTornado.Chat;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.LLM;
using Mute.Moe.Services.LLM.Memory;
using System.Threading.Tasks;
using Mute.Moe.Services.LLM.Memory.Extraction;

namespace Mute.Moe.Discord.Modules.Personality;

[UsedImplicitly]
[RequireOwner]
public partial class Dev(IConversationStateStorage convState, IAgentMemoryStorage memory, AgentMemoryConfidenceDecayOverTime decay, FactExtractionService factExtract, LlmFactModel factModel, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    : MuteBaseModule
{
    [Command("create-test-mem")]
    [UsedImplicitly]
    public async Task Create()
    {
        await memory.CreateMemory(123, "the quick brown fox jumps over the lazy dog", 1);
        await ReplyAsync("Done");
    }

    [Command("query-test-mem")]
    [UsedImplicitly]
    public async Task Query(string query)
    {
        var items = (await memory.FindSimilar(123, "query", 10)).ToList();

        foreach (var agentMemory in items)
            await ReplyAsync(agentMemory.Text);
        await ReplyAsync("Done");
    }

    [Command("decay-test-mem")]
    [UsedImplicitly]
    public async Task Decay(float threshold, float factor)
    {
        await decay.ApplyDecay(threshold, factor);
    }

    [Command("test-fact-extraction2")]
    [UsedImplicitly]
    public async Task FactExtraction2()
    {
        var channel = Context.Channel;

        var state = await convState.Get(channel.Id);
        if (state == null)
        {
            await ReplyAsync("No stored conversation state for that channel");
            return;
        }

        var c = new ChatConversation(new ChatRequest(), null!, null, null!);
        c.Load(state.Json);

        var facts = await factExtract.Extract(c.Transcript("Assistant"));

        await LongReplyAsync("## Facts:\n" + string.Join("\n", facts.Select(a => $" - {a}")));
    }
}