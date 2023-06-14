using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Context.Preprocessing;
using Mute.Moe.Extensions;
using Mute.Moe.Services.LLM;

namespace Mute.Moe.Discord.Services.Responses.Enigma;

/// <summary>
/// Process every non-command message sent in any channel
/// </summary>
public class EnigmaResponse
    : IConversationPreprocessor
{
    private readonly ILargeLanguageModel _llm;

    private readonly ConcurrentDictionary<ulong, ChannelState> _channelState = new();
    public int Count => _channelState.Count;

    public EnigmaResponse(ILargeLanguageModel llm)
    {
        _llm = llm;
    }

    public async Task Process(MuteCommandContext context)
    {
        var state = GetState(context.Channel);

        if (state.TryReply(EnigmaMessage.From(context)) is string reply)
            await context.Channel.TypingReplyAsync(reply);
    }

    private ChannelState CreateContext(ulong key)
    {
        return new ChannelState(key, _llm);
    }

    public ChannelState GetState(IChannel channel)
    {
        return _channelState.GetOrAdd(channel.Id, CreateContext);
    }

    public IReadOnlyList<ChannelState> GetStates()
    {
        return _channelState.Select(a => a.Value).ToList();
    }
}