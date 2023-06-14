using System;
using Discord.WebSocket;
using Humanizer;
using Mute.Moe.Services.LLM;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Services.Responses.Enigma;

public class ChannelState
{
    public ulong ChannelId { get; }

    private readonly ILargeLanguageModel _llm;

    private readonly HashSet<ulong> _participants = new();
    public IReadOnlyList<ulong> Participants => _participants.ToList();

    public DateTime LastUpdate { get; private set; }

    public ChannelState(ulong channelId, ILargeLanguageModel llm)
    {
        ChannelId = channelId;
        _llm = llm;

        LastUpdate = DateTime.UtcNow;
    }

    public string? TryReply(EnigmaMessage message, bool updateState = true)
    {
        if (updateState)
        {
            LastUpdate = DateTime.UtcNow;
            _participants.Add(message.Speaker);
        }

        return null;
    }

    #region stringify
    public override string ToString()
    {
        var updated = LastUpdate.Humanize();
        return $"{ChannelId} updated {updated}, {Participants.Count} participants";
    }

    public string ToString(DiscordSocketClient client, bool guildName)
    {
        var updated = LastUpdate.Humanize();
        var channel = client.GetChannel(ChannelId)?.Name(guildName);

        var participantsWord = "participant" + (Participants.Count == 1 ? "" : "s");

        return $"`{channel ?? "Unknown"}` updated {updated}, {Participants.Count} {participantsWord}";
    }
    #endregion
}