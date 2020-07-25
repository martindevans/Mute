using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Oddity.Models.Cores;
using Oddity.Models.Launches;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class DetailedCoreInfoExtensions
    {
        public static async Task<EmbedBuilder> AugmentDiscordEmbed(this CoreInfo info, EmbedBuilder builder)
        {
            if (info.Launches == null)
                return builder;

            var missions = info.Launches.Select(a => a.Value).Where(a => a != null).ToArray();

            if (missions.Length > 0)
                builder = builder.WithDescription(string.Join("\n", missions.Select(MissionLine)));

            return builder;
        }

        public static async Task<EmbedBuilder> DiscordEmbed(this CoreInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            //Choose color based on status
            Color color;
            switch (info.Status)
            {
                case CoreStatus.Active:
                    color = Color.Green;
                    break;
                case CoreStatus.Expended:
                case CoreStatus.Lost:
                    color = Color.Red;
                    break;
                case CoreStatus.Retired:
                case CoreStatus.Inactive:
                    color = Color.DarkGrey;
                    break;
                case CoreStatus.Unknown:
                default:
                    color = Color.Purple;
                    break;
            }

            //Create text description
            var description = "";
            if (info.Launches != null && info.Launches.Count > 0)
            {
                var ms = string.Join(", ", info.Launches.Select(m => $"{m.Value.Name} (Flight {m.Value.FlightNumber})").ToArray());
                description = $"This core has flown {info.Launches.Count} missions; {ms}. ";
            }

            var builder = new EmbedBuilder()
                          .WithTitle(info.Serial)
                          .WithDescription(description)
                          .WithColor(color)
                          .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (info.Block.HasValue)
                builder = builder.AddField("Block", info.Block.Value.ToString(), true);

            if (info.AsdsLandings.HasValue && info.AsdsLandings.Value > 0)
                builder = builder.AddField("ASDS Landings", info.AsdsLandings.Value.ToString(), true);

            if (info.RtlsLandings.HasValue && info.RtlsLandings.Value > 0)
                builder = builder.AddField("RTLS Landings", info.RtlsLandings.Value.ToString(), true);

            if (info.Launches != null && info.Launches.Any())
                builder = builder.AddField("First Launch Date", info.Launches.First().Value.DateUtc!.Value.ToString("HH\\:mm UTC dd-MMM-yyyy"), true);

            if (info.Launches != null && info.Launches.Count > 1)
                builder = builder.WithDescription(string.Join("\n", info.Launches.Select(a => a.Value).Select(CoreMissionLine)));

            return builder;
        }

        private static string MissionLine(LaunchInfo mission)
        {
            var fname = $"{mission.Name}";

            var url = mission.Links.Wikipedia ?? mission.Links.Reddit.Campaign ?? mission.Links.Reddit.Launch ?? mission.Links.Presskit;
            if (url != null)
                fname = $"[{fname}]({url})";

            var txt = $" • ({mission.FlightNumber}) {fname}";

            if (mission.DateUtc.HasValue)
            {
                var ago = (DateTime.UtcNow - mission.DateUtc.Value).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Hour);
                txt += $" {ago} ago";
            }

            if (mission.Success.HasValue)
                if (!mission.Success.Value)
                    txt += " ❌";

            return txt;
        }

        private static string CoreMissionLine(LaunchInfo mission)
        {
            return $" • ({mission.FlightNumber}) {mission.Name}";
        }
    }
}
