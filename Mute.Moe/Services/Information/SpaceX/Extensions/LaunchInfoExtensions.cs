using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using JetBrains.Annotations;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class LaunchInfoExtensions
    {
        [ItemNotNull, NotNull] public static async Task<EmbedBuilder> DiscordEmbed([NotNull] this Task<LaunchInfo> launch)
        {
            return (await launch).DiscordEmbed();
        }

        [NotNull] public static EmbedBuilder DiscordEmbed([NotNull] this LaunchInfo launch)
        {
            var icon = launch.Links.MissionPatch ?? launch.Links.MissionPatchSmall;
            var url = launch.Links.Wikipedia ?? launch.Links.RedditCampaign ?? launch.Links.RedditLaunch ?? launch.Links.Presskit;
            var upcoming = launch.Upcoming.HasValue && launch.Upcoming.Value;
            var success = launch.LaunchSuccess.HasValue && launch.LaunchSuccess.Value;

            var builder = new EmbedBuilder()
                          .WithTitle(launch.MissionName)
                          .WithDescription(launch.Details)
                          .WithUrl(url)
                          .WithAuthor($"Flight {launch.FlightNumber}", icon, icon)
                          .WithColor(upcoming ? Color.Blue : success ? Color.Green : Color.Red)
                          .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            var site = launch.LaunchSite.SiteLongName ?? launch.LaunchSite.SiteName;
            if (!string.IsNullOrWhiteSpace(site))
                builder = builder.AddField("Launch Site", site);

            if (launch.LaunchDateUtc.HasValue)
            {
                builder = builder.AddField("Launch Date", launch.LaunchDateUtc.Value.ToString("HH\\:mm UTC dd-MMM-yyyy"), true);
                builder = builder.AddField("T-", (launch.LaunchDateUtc.Value - DateTime.UtcNow).Humanize());
            }

            var landing = string.Join(", ", launch.Rocket.FirstStage.Cores.Select(c => c.LandingVehicle?.ToString()).Where(a => a != null).ToArray());
            if (!string.IsNullOrWhiteSpace(landing))
                builder = builder.AddField("Landing", landing, true);

            var serials = string.Join(",", launch.Rocket.FirstStage.Cores.Select(c => c.CoreSerial ?? "B????").ToArray());
            builder = builder.AddField("Vehicle", $"{launch.Rocket.RocketName} ({serials})", true);

            var flights = string.Join(",", launch.Rocket.FirstStage.Cores.Select(c => (c.Flight - 1)?.ToString() ?? "??").ToArray());
            builder = builder.AddField("Previous Flights", flights);

            return builder;
        }

        [NotNull] public static string Summary([NotNull] this LaunchInfo launch)
        {
            var date = launch.LaunchDateUtc.HasValue ? launch.LaunchDateUtc.Value.Humanize() : "";
            var num = launch.FlightNumber;
            var site = launch.LaunchSite;
            var name = launch.MissionName;
            var reuse = launch.Reuse;
            var type = launch.Rocket.RocketName;
            return $"Flight {num} will launch '{name}' from {site.SiteName} on a{(reuse.Core ?? false ? " reused" : "")} {type} rocket {date}";
        }
    }
}
