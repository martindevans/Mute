using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Responses.Enigma;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
[RequireOwner]
[Group("enigma")]
public class Enigma
    : BaseModule
{
    private readonly DiscordSocketClient _client;
    private readonly EnigmaResponse _enigma;

    public Enigma(DiscordSocketClient client, EnigmaResponse enigma)
    {
        _client = client;
        _enigma = enigma;
    }

    #region status
    [Command("status"), Summary("I will show the status of my current enigma conversation")]
    public async Task Status(IChannel channel)
    {
        await TypingReplyAsync($"There are {_enigma.Count} total active conversations");

        var state = _enigma.GetState(channel);
        await ReplyAsync(state.ToString(_client, false));
    }

    [Command("status"), Summary("I will show the status of my current enigma conversations")]
    [RequireOwner]
    public async Task Status(bool all)
    {
        if (!all)
        {
            await Status(Context.Message.Channel);
            return;
        }

        var groups = _enigma
            .GetStates()
            .GroupBy(a => TryGetChannelGuild(a.ChannelId))
            .ToList();

        var noGuild = groups.Where(a => a.Key == null);
        var hasGuild = groups.Where(a => a.Key != null);

        var builder = new StringBuilder();
        foreach (var group in noGuild.Concat(hasGuild))
        {
            var title = group.Key == null ? " # No Guild" : $" # {group.Key.Name}";
            builder.AppendLine($"**{title}**");

            foreach (var chan in group)
                builder.AppendLine($"  - {chan.ToString(_client, false)}");
            builder.AppendLine();
        }

        await ReplyAsync(builder.ToString());
    }

    [Command("status"), Summary("I will show the status of my current enigma conversations")]
    [UsedImplicitly]
    public Task Status()
    {
        return Status(false);
    }
    #endregion

    #region query
    [Command("query"), Summary("Query the conversation state for a channel")]
    public async Task Query(IChannel channel, [Remainder, UsedImplicitly] string msg)
    {
        var state = _enigma.GetState(channel);

        var r = state.TryReply(EnigmaMessage.From(Context.User.Id, msg), false);
        if (string.IsNullOrEmpty(r))
            r = "Null reply.";

        await TypingReplyAsync(r);
    }

    [Command("query"), Summary("Query the conversation state for a channel")]
    [UsedImplicitly]
    public Task Query([Remainder] string message)
    {
        return Query(Context.Channel, message);
    }
    #endregion

    #region helpers
    private IGuild? TryGetChannelGuild(ulong channel)
    {
        return (_client.GetChannel(channel) as IGuildChannel)?.Guild;
    }
    #endregion
}