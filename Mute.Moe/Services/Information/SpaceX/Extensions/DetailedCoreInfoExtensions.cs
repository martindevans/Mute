using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;

using Oddity.API.Models.DetailedCore;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class DetailedCoreInfoExtensions
    {
        public static async Task<EmbedBuilder> AugmentDiscordEmbed( this DetailedCoreInfo info, EmbedBuilder builder, ISpacexInfo spacex)
        {
            var missions = await info.Missions.ToAsyncEnumerable().Select(async a => (await spacex.Launch(a.Flight)).FirstOrDefault()).Where(a => a != null).ToArrayAsync();
            await Task.WhenAll(missions);

            if (missions.Length > 0)
                builder = builder.WithDescription(string.Join("\n", missions.Select(a => MissionLine(a.Result))));

            return builder;
        }

        public static async Task<EmbedBuilder> DiscordEmbed( this DetailedCoreInfo info)
        {
            //Choose color based on status
            Color color;
            switch (info.Status)
            {
                case DetailedCoreStatus.Active:
                    color = Color.Green;
                    break;
                case DetailedCoreStatus.Lost:
                    color = Color.Red;
                    break;
                case DetailedCoreStatus.Inactive:
                    color = Color.DarkGrey;
                    break;
                case DetailedCoreStatus.Unknown:
                case null:
                default:
                    color = Color.Purple;
                    break;
            }

            //Create text description
            var description = "";
            if (info.Missions.Count > 0)
            {
                var ms = string.Join(", ", info.Missions.Select(m => $"{m.Name} (Flight {m.Flight})").ToArray());
                description = $"This core has flown {info.Missions.Count} missions; {ms}. ";
            }
            description += info.Details ?? "";

            var builder = new EmbedBuilder()
                          .WithTitle(info.CoreSerial)
                          .WithDescription(description)
                          .WithColor(color)
                          .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (info.Block.HasValue)
                builder = builder.AddField("Block", info.Block.Value.ToString(), true);

            if (info.AsdsLandings.HasValue && info.AsdsLandings.Value > 0)
                builder = builder.AddField("ASDS Landings", info.AsdsLandings.Value.ToString(), true);

            if (info.RtlsLandings.HasValue && info.RtlsLandings.Value > 0)
                builder = builder.AddField("RTLS Landings", info.RtlsLandings.Value.ToString(), true);

            if (info.OriginalLaunch.HasValue)
                builder = builder.AddField("First Launch Date", info.OriginalLaunch.Value.ToString("HH\\:mm UTC dd-MMM-yyyy"), true);

            if (info.Missions.Count > 1)
                builder = builder.WithDescription(string.Join("\n", info.Missions.Select(MissionLine)));

            return builder;
        }

        private static string MissionLine(LaunchInfo mission)
        {
            var fname = $"{mission.MissionName}";

            var url = mission.Links.Wikipedia ?? mission.Links.RedditCampaign ?? mission.Links.RedditLaunch ?? mission.Links.Presskit;
            if (url != null)
                fname = $"[{fname}]({url})";

            var txt = $" • ({mission.FlightNumber}) {fname}";

            if (mission.LaunchDateUtc.HasValue)
            {
                var ago = (DateTime.UtcNow - mission.LaunchDateUtc.Value).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Hour);
                txt += $" {ago} ago";
            }

            if (mission.LaunchSuccess.HasValue && !mission.LaunchSuccess.Value)
                txt += " ❌";

            return txt;
        }

        private static string MissionLine(CoreMissionInfo mission)
        {
            return $" • ({mission.Flight}) {mission.Name}";
        }
    }
}
