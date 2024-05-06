using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Services.Notifications.RSS;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("rss")]
public class Rss(IRssNotifications _rss)
        : BaseModule
{
    [Command("subscribe"), Summary("I will subscribe this channel to RSS updates")]
    [UsedImplicitly]
    public Task Subscribe(string url)
    {
        return _rss.Subscribe(url, Context.Channel.Id, null);
    }
}