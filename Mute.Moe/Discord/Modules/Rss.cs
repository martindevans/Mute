using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Moe.Services.Notifications.RSS;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("rss")]
public class Rss(IRssNotifications _rss, DiscordSocketClient _client)
        : BaseModule
{
    [Command("subscribe"), Summary("I will subscribe this channel to RSS updates")]
    [UsedImplicitly]
    public Task Subscribe(string url)
    {
        return _rss.Subscribe(url, Context.Channel.Id, null);
    }

    [RequireOwner, Command("list"), Summary("I will list all RSS subscriptions")]
    [UsedImplicitly]
    public async Task List()
    {
        // Note that this might leak private subs in other channels or other guilds. That's why it is owner only.

        var subs = await _rss.GetSubscriptions().ToListAsync();
        await DisplayItemList(
            subs,
            () => "No active RSS subscriptions",
            item => TypingReplyAsync($"`{item.FeedUrl}` in {_client.GetChannel(item.Channel).Name()}"),
            items => $"There are {items.Count} active RSS subscriptions:",
            (item, _) => $"`{item.FeedUrl}` in {_client.GetChannel(item.Channel).Name()}"
        );
    }
}