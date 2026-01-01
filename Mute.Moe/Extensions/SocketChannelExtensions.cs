using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="SocketChannel"/>
/// </summary>
public static class SocketChannelExtensions
{
    /// <summary>
    /// Get the name for a channel. Properly handles DM, Group, guild or deleted channels
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="guildName"></param>
    /// <returns></returns>
    public static string Name(this SocketChannel channel, bool guildName = true)
    {
        return channel switch
        {
            IDMChannel dm => $"#DM @{dm.Recipient.Username}",
            SocketGroupChannel grp => $"#GRP {grp.Name}",
            SocketGuildChannel gc => guildName ? $"{gc.Guild.Name}#{gc.Name}" : $"#{gc.Name}",

            null => "NULL (Deleted Channel?)",
            _ => $"UNK {channel.GetChannelType()} {channel.Id}",
        };
    }
}