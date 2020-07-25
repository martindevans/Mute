using System;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Mute.Moe.Extensions;
using Oddity.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class RoadsterInfoExtensions
    {
        public static async Task<EmbedBuilder> DiscordEmbed(this Task<RoadsterInfo> roadster, Random rng)
        {
            return (await roadster).DiscordEmbed(rng);
        }

        public static EmbedBuilder DiscordEmbed(this RoadsterInfo roadster, Random rng)
        {
            var embed = new EmbedBuilder()
                        .WithTitle($"NORAD:{roadster.NoradId}")
                        .WithDescription($"{roadster.Name} was put into space {roadster.DateTimeUtc!.Humanize()}.")
                        .WithUrl(roadster.Wikipedia)
                        .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (roadster.SpeedKph.HasValue)
                embed.AddField("Speed", $"{roadster.SpeedKph}Kph", true);

            embed.AddField("Orbit", roadster.OrbitType, true);

            if (roadster.PeriodDays.HasValue)
                embed.AddField("Period", roadster.PeriodDays.Value.Days().Humanize(), true);

            if (roadster.EarthDistanceKilometers.HasValue)
                embed.AddField("Distance From Earth", $"{(int)roadster.EarthDistanceKilometers:#,##0}Km", true);

            if (roadster.MarsDistanceKilometers.HasValue)
                embed.AddField("Distance From Mars", $"{(int)roadster.MarsDistanceKilometers:#,##0}Km", true);

            if (roadster.EarthDistanceKilometers.HasValue && roadster.MarsDistanceKilometers.HasValue)
                embed.WithColor(roadster.EarthDistanceKilometers.Value < roadster.MarsDistanceKilometers.Value ? Color.Blue : Color.Red);

            if (roadster.FlickrImages.Count > 0)
                embed.WithImageUrl(roadster.FlickrImages.Random(rng));

            return embed;
        }
    }
}
