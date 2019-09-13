using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Services.Notifications.RSS;

namespace Mute.Moe.Discord.Modules
{
    [Group("rss")]
    public class Rss
        : BaseModule
    {
        private readonly IRssNotifications _rss;

        public Rss(IRssNotifications rss)
        {
            _rss = rss;
        }

        [Command("subscribe"), Summary("I will subscribe this channel to RSS updates")]
        public async Task Subscribe(string url)
        {
            await _rss.Subscribe(url, Context.Channel.Id, null);
        }
    }
}
