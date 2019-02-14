using System.Threading.Tasks;
using Discord;
using Humanizer;
using JetBrains.Annotations;
using Oddity.API.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class RoadsterInfoExtensions
    {
        [NotNull, ItemNotNull] public static async Task<EmbedBuilder> DiscordEmbed([NotNull] this Task<RoadsterInfo> roadster)
        {
            return (await roadster).DiscordEmbed();
        }

        [NotNull] public static EmbedBuilder DiscordEmbed([NotNull] this RoadsterInfo roadster)
        {
            var speed = ((int)roadster.SpeedKph).ToString("#,##0");
            var earth = ((int)roadster.EarthDistanceKilometers);
            var mars = ((int)roadster.MarsDistanceKilometers);
            var period = ((int)roadster.PeriodDays).Days().Humanize();

            return new EmbedBuilder()
                 .WithTitle($"NORAD:{roadster.NoradId}")
                 .WithDescription($"{roadster.Name} was put into space {roadster.DateTimeUtc.ToLocalTime().Humanize()}.")
                 .WithUrl(roadster.Wikipedia)
                 .WithColor(earth < mars ? Color.Blue : Color.Red)
                 .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API")
                 .AddField("Speed", $"{speed}Kph", true)
                 .AddField("Orbit", roadster.OrbitType, true)
                 .AddField("Period", period, true)
                 .AddField("Distance From Earth", $"{earth:#,##0}Km", true)
                 .AddField("Distance From Mars", $"{mars:#,##0}Km", true);
        }
    }
}
