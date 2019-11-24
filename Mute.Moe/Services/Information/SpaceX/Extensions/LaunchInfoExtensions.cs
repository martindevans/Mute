using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Configuration;
using JetBrains.Annotations;
using Oddity.API.Models.Launch;
using Oddity.API.Models.Launch.Rocket.FirstStage;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class LaunchInfoExtensions
    {
        [ItemNotNull, NotNull] public static async Task<EmbedBuilder> DiscordEmbed([NotNull, ItemCanBeNull] this Task<LaunchInfo> launch)
        {
            return (await launch).DiscordEmbed();
        }

        [NotNull] public static EmbedBuilder DiscordEmbed([CanBeNull] this LaunchInfo launch)
        {
            var builder = new EmbedBuilder()
                .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (launch == null)
            {
                builder = builder.WithTitle("No Upcoming Missions!");
                return builder;
            }

            var icon = launch.Links.MissionPatch ?? launch.Links.MissionPatchSmall;
            var url = launch.Links.Wikipedia ?? launch.Links.RedditCampaign ?? launch.Links.RedditLaunch ?? launch.Links.Presskit;
            var upcoming = launch.Upcoming.HasValue && launch.Upcoming.Value;
            var success = launch.LaunchSuccess.HasValue && launch.LaunchSuccess.Value;

            builder = builder
                .WithTitle(launch.MissionName)
                .WithDescription(launch.Details)
                .WithUrl(url)
                .WithAuthor($"Flight {launch.FlightNumber}", icon, icon)
                .WithColor(upcoming ? Color.Blue : success ? Color.Green : Color.Red);

            var site = launch.LaunchSite.SiteLongName ?? launch.LaunchSite.SiteName;
            if (!string.IsNullOrWhiteSpace(site))
                builder = builder.AddField("Launch Site", site);

            if (launch.LaunchDateUtc.HasValue)
            {
                var date = launch.LaunchDateUtc.Value.ToString("HH\\:mm UTC dd-MMM-yyyy");
                if (launch.IsTentative || launch.LaunchDateUtc.Value < DateTime.UtcNow)
                {
                    builder.AddField("Launch Date", $"Uncertain  - scheduled for {date} ± 1 {launch.TentativeMaxPrecision}");
                }
                else
                {
                    builder = builder.AddField("Launch Date", date, true);
                    builder = builder.AddField("T-", (launch.LaunchDateUtc.Value - DateTime.UtcNow).Humanize());
                }
            }

            var landing = string.Join(", ", launch.Rocket.FirstStage.Cores.Select(c => c.LandingVehicle?.ToString()).Where(a => a != null).ToArray());
            if (!string.IsNullOrWhiteSpace(landing))
                builder = builder.AddField("Landing", landing, true);

            var serials = string.Join(",", launch.Rocket.FirstStage.Cores.Select(c => c.CoreSerial ?? "B????").ToArray());
            builder = builder.AddField("Vehicle", $"{launch.Rocket.RocketName} ({serials})", true);

            string PreviousFlights(CoreInfo core)
            {
                //If we know it's not re-used, return zero
                var reused = core.Reused ?? true;
                if (!reused)
                    return "0";

                //Return the flight number
                //There seems to be some inconsistency between if the very first flight is zero or one, sub one and make sure it doesn't underflow
                var flight = core.Flight;
                if (flight.HasValue)
                    return Math.Max(flight.Value - 1, 0).ToString();

                return "??";
            }

            var flights = string.Join(",", launch.Rocket.FirstStage.Cores.Select(PreviousFlights).ToArray());
            builder = builder.AddField("Previous Flights", flights);

            return builder;
        }

        [NotNull] public static string Summary([NotNull] this LaunchInfo launch)
        {
            var date = DateString(launch.LaunchDateUtc, launch.TentativeMaxPrecision);
            var num = launch.FlightNumber;
            var site = launch.LaunchSite;
            var name = launch.MissionName;
            var reuse = launch.Reuse;
            var type = launch.Rocket.RocketName;
            return $"Flight {num}: `{name}` from `{site.SiteName}` on a{(reuse.Core ?? false ? " reused" : "")} {type} rocket {date}";
        }

        private static string DateString(DateTime? date, TentativeMaxPrecision? precision)
        {
            if (!date.HasValue)
                return "";

            if (!precision.HasValue)
                return date.Value.Humanize();

            switch (precision.Value)
            {
                case TentativeMaxPrecision.Hour:
                    if ((date.Value - DateTime.UtcNow) > TimeSpan.FromDays(1))
                        return date.Value.Humanize();
                    else
                        return $"about {date.Value.Humanize()}";

                case TentativeMaxPrecision.Day:
                    return $"on {date.Value.ToString("m")}";

                case TentativeMaxPrecision.Month:
                    return $"in {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Value.Month)} {date.Value.Year}";

                case TentativeMaxPrecision.Quarter:
                    return $"in Q{date.Value.Month / 4 + 1} {date.Value.Year}";

                case TentativeMaxPrecision.Half:
                    return $"in H{date.Value.Month / 6 + 1} {date.Value.Year}";

                case TentativeMaxPrecision.Year:
                    return $"in {date.Value.Year.ToString()}";

                default: throw new NotSupportedException($"Unknown tenatative date time `{precision.Value}`");
            }
        }
    }
}
