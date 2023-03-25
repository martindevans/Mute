using System;
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
        var state = _channelState.GetOrAdd(context.Channel.Id, CreateContext);

        if (state.TryReply(context.Message.CleanContent) is string reply)
            await context.Channel.TypingReplyAsync(reply);
    }

    private ChannelState CreateContext(ulong key)
    {
        var seed = HashCode.Combine(key, DateTime.UtcNow);
        return new ChannelState(_llm, seed);
    }

    public ChannelState? GetState(IChannel channel)
    {
        _channelState.TryGetValue(channel.Id, out var value);
        return value;
    }
}