using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Extensions
{
    public static class SocketChannelExtensions
    {
        public static string Name(this SocketChannel channel, bool guildName = true)
        {
            return channel switch
            {
                IDMChannel dm => $"#DM @{dm.Recipient.Username}",
                SocketGroupChannel grp => $"#GRP {grp.Name}",
                SocketGuildChannel gc => guildName ? $"{gc.Guild.Name}#{gc.Name}" : $"#{gc.Name}",

                _ => $"UNK {channel.GetChannelType()} {channel.Id}",
            };
        }
    }
}
