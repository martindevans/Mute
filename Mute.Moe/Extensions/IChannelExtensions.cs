using Discord;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IChannel"/>
/// </summary>
public static class IChannelExtensions
{
    /// <summary>
    /// Get the context ID to use for memory storage and retrieval in this channel
    /// </summary>
    /// <returns></returns>
    public static ulong GetAgentMemoryContextId(this IChannel channel)
    {
        if (channel is IGuildChannel gc)
            return gc.GuildId;
        return channel.Id;
    }
}