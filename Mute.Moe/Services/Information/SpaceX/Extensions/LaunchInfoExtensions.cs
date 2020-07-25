using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Oddity.Models.Launches;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class LaunchInfoExtensions
    {
        public static async Task<EmbedBuilder> DiscordEmbed(this Task<LaunchInfo?> launch)
        {
            return (await launch).DiscordEmbed();
        }

        public static EmbedBuilder DiscordEmbed(this LaunchInfo? launch)
        {
            var builder = new EmbedBuilder()
                .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (launch == null)
            {
                builder = builder.WithTitle("No Upcoming Missions!");
                return builder;
            }

            var icon = launch.Links.Patch.Large ?? launch.Links.Patch.Small;
            var url = launch.Links.Wikipedia ?? launch.Links.Reddit.Campaign ?? launch.Links.Reddit.Launch ?? launch.Links.Presskit;
            var upcoming = launch.Upcoming.HasValue && launch.Upcoming.Value;
            var success = launch.Success.HasValue && launch.Success.Value;

            builder = builder
                .WithTitle(launch.Name)
                .WithDescription(launch.Details)
                .WithUrl(url)
                .WithAuthor($"Flight {launch.FlightNumber}", icon, icon)
                .WithColor(upcoming ? Color.Blue : success ? Color.Green : Color.Red);

            var site = launch.Launchpad.Value.FullName ?? launch.Launchpad.Value.Name;
            if (!string.IsNullOrWhiteSpace(site))
                builder = builder.AddField("Launch Site", site);

            if (launch.DateUtc.HasValue)
            {
                var date = launch.DateUtc.Value.ToString("HH\\:mm UTC dd-MMM-yyyy");

                if (launch.NotEarlierThan ?? false)
                {
                    builder.AddField("Launch Date", $"NET {date}");
                }
                else if (launch.DatePrecision.HasValue)
                {
                    builder.AddField("Launch Date", $"Uncertain  - scheduled for {date} ± 1 {launch.DatePrecision}");
                }
                else
                {
                    builder = builder.AddField("Launch Date", date, true);
                    builder = builder.AddField("T-", (launch.DateUtc.Value - DateTime.UtcNow).Humanize());
                }
            }

            if (launch.Cores.Any(a => a.Landpad.Value != null))
                builder.AddField("Landing Pad", string.Join(",", launch.Cores.Select(a => a.Landpad.Value.Name)), true);

            builder.AddField("Vehicle", string.Join(",", launch.Cores.Select(a => $"{a.Core.Value.Serial}")), true);

            builder.AddField("Previous Flights", string.Join(",", launch.Cores.Select(a => a.Core.Value.ReuseCount).Where(a => a.HasValue)));

            return builder;
        }

        public static string Summary(this LaunchInfo launch)
        {
            var date = DateString(launch.DateUtc, launch.DatePrecision);
            var num = launch.FlightNumber;
            var site = launch.Launchpad.Value.FullName;
            var name = launch.Name;
            var type = launch.Rocket.Value.Name;
            return $"Flight {num}: `{name}` from `{site}` on a {type} rocket {date}";
        }

        private static string DateString(DateTime? date, DatePrecision? precision)
        {
            if (!date.HasValue)
                return "";

            if (!precision.HasValue)
                return date.Value.Humanize();

            switch (precision.Value)
            {
                case DatePrecision.Hour:
                    if ((date.Value - DateTime.UtcNow) > TimeSpan.FromDays(1))
                        return date.Value.Humanize();
                    else
                        return $"about {date.Value.Humanize()}";

                case DatePrecision.Day:
                    return $"on {date.Value:m}";

                case DatePrecision.Month:
                    return $"in {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Value.Month)} {date.Value.Year}";

                case DatePrecision.Quarter:
                    return $"in Q{date.Value.Month / 4 + 1} {date.Value.Year}";

                case DatePrecision.Half:
                    return $"in H{date.Value.Month / 6 + 1} {date.Value.Year}";

                case DatePrecision.Year:
                    return $"in {date.Value.Year}";

                case DatePrecision.Unknown:
                default:
                    throw new NotSupportedException($"Unknown tenatative date time `{precision.Value}`");
            }
        }
    }
}
