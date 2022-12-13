using System.Threading.Tasks;
using Discord;

using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Music.Extensions;

// ReSharper disable once InconsistentNaming
public static class ITrackExtensions
{
    public static async Task<EmbedBuilder> DiscordEmbed( this ITrack track)
    {
        var embed = new EmbedBuilder()
            .WithTitle(track.Title)
            .WithDescription($"Duration: {track.Duration.Minutes}m{track.Duration.Seconds}s")
            .WithFooter($"{track.ID.MeaninglessString()}");

        if (!string.IsNullOrWhiteSpace(track.Url))
            embed = embed.WithUrl(track.Url);

        if (!string.IsNullOrWhiteSpace(track.ThumbnailUrl))
            embed = embed.WithThumbnailUrl(track.ThumbnailUrl);

        if (!string.IsNullOrWhiteSpace(track.Url))
            embed = embed.WithUrl(track.Url);

        return embed;
    }
}