using Discord;
using Discord.Commands;
using Mute.Moe.Services.Archive;
using System.Text;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Introspection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[RequireOwner]
[Group("archive")]
public class ArchiveHoles(IChatArchiveHoles _holes, IChatArchive _archive)
    : MuteBaseModule
{
    [Command("holes"), Summary("I will show the archive holes for the current channel, or every channel in the current server")]
    [UsedImplicitly]
    public async Task ChannelHoles()
    {
        if (Context.Guild is { } guild)
        {
            var builder = new StringBuilder();
            foreach (var channel in guild.Channels)
            {
                if (channel is not IMessageChannel msgChannel)
                    continue;

                var channelHoles = await _holes.List(channel.Id);

                if (channelHoles.Count > 0)
                {
                    builder.Append($"**#{channel.Name}** (`{channel.Id}`): {channelHoles.Count} hole{(channelHoles.Count == 1 ? "" : "s")}\n");
                    foreach (var hole in channelHoles)
                        builder.Append($"  {await FormatMessage(hole, msgChannel)}\n");
                }
            }

            if (builder.Length == 0)
                await ReplyAsync($"No archive holes in {guild.Name}");
            else
                await LongReplyAsync(builder.ToString().TrimEnd());
        }
        else
        {
            var holeMessages = await (await _holes.List(Context.Channel.Id))
                .ToAsyncEnumerable()
                .Select(async (a, _, _) => await FormatHole(a, Context.Channel))
                .ToArrayAsync();
            
            await DisplayItemList(
                holeMessages,
                $"No archive holes for #{Context.Channel.Id}",
                async item => await ReplyAsync(item),
                items => $"{items.Count} archive hole{(items.Count == 1 ? "" : "s")} in #{Context.Channel.Id}:",
                (item, _) => item
            );
        }
    }

    [Command("count"), Summary("I will tell you how many messages I have archived for this context")]
    [UsedImplicitly]
    public async Task Count()
    {
        await Count(null, null);
    }

    [Command("count"), Summary("I will tell you how many messages I have archived for this context, filtered to a specific user")]
    [UsedImplicitly]
    public async Task Count(IUser? user)
    {
        await Count(user, null);
    }

    [Command("count"), Summary("I will tell you how many messages I have archived for this context, filtered to a specific channel")]
    [UsedImplicitly]
    public async Task Count(IChannel channel)
    {
        await Count(null, channel);
    }

    [Command("count"), Summary("I will tell you how many messages I have archived for this context, filtered to a specific user in a specific channel")]
    [UsedImplicitly]
    public async Task Count(IUser? user, IChannel? channel)
    {
        var count = _archive.Count(Context.AgentMemoryContextId, channel?.Id, user?.Id);

        var parts = new List<string>();
        if (user != null)
            parts.Add($"by {user.Mention}");
        if (channel != null)
            parts.Add($"in #{channel.Id}");

        var scope = parts.Count == 0 ? "in this context" : string.Join(" ", parts);

        await ReplyAsync($"I have {count} archived message{(count == 1 ? "" : "s")} {scope}");
    }

    private async Task<string> FormatHole(ChatArchiveHole hole, IMessageChannel channel)
    {
        return $"#{hole.ChannelId} from {await FormatMessage(hole, channel)}";
    }

    private async Task<string> FormatMessage(ChatArchiveHole hole, IMessageChannel channel)
    {
        var message = await channel.GetMessageAsync(hole.StartMessageId);
        var date = message?.CreatedAt.ToString("g") ?? "Unknown Date";
        
        var text = Context.Client.GetChannel(hole.ChannelId) is IGuildChannel gc
            ? $"[`{date}`](https://discord.com/channels/{gc.GuildId}/{hole.ChannelId}/{hole.StartMessageId})"
            : $"`{date}`";

        return $"{text} ({(hole.Forward ? "forward" : "backward")})";
    }
}
