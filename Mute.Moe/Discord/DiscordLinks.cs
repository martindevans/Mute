using Discord;

namespace Mute.Moe.Discord;

/// <summary>
/// Provides utility methods for generating Discord links to messages and channels.
/// </summary>
public class DiscordLinks
{
    /// <summary>
    /// Link directly to a specific message
    /// </summary>
    /// <param name="guild"></param>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private static string Message(ulong guild, ulong channel, ulong message)
    {
        return $"https://discord.com/channels/{guild}/{channel}/{message}";
    }

    /// <summary>
    /// Link directly to a specific message
    /// </summary>
    /// <param name="guild"></param>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static string Message(IGuild guild, IChannel channel, IMessage message)
    {
        return Message(guild.Id, channel.Id, message.Id);
    }

    /// <summary>
    /// Link directly to a specific message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static string Message(IMessage message)
    {
        var channel = message.Channel;

        if (channel is IGuildChannel gc)
            return Message(gc.Guild, gc, message);

        // Other non-guild channels ignore the guild part
        return Message(0, channel.Id, message.Id);
    }

    /// <summary>
    /// Link directly to a channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public static string Channel(IChannel channel)
    {
        if (channel is IGuildChannel gc)
            return $"https://discord.com/channels/{gc.Guild.Id}/{channel.Id}";

        // Other non-guild formats duplicate the channel ID to the "guild" ID
        return $"https://discord.com/channels/0/{channel.Id}";
    }
}