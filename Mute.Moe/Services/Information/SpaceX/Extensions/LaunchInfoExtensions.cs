using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Information.SpaceX.Extensions;

public static class LaunchInfoExtensions
{
    public static async Task<EmbedBuilder> DiscordEmbed(this Task<ILaunchInfo?> launch, Random rng)
    {
        return (await launch).DiscordEmbed(rng);
    }

    public static EmbedBuilder DiscordEmbed(this ILaunchInfo? launch, Random rng)
    {
        var builder = new EmbedBuilder()
            .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

        if (launch == null)
        {
            builder = builder.WithTitle("No Upcoming Missions!");
            return builder;
        }

        var upcoming = launch.Upcoming;
        var success = launch.Success.HasValue && launch.Success.Value;

        builder = builder
            .WithTitle(launch.Name)
            .WithDescription(launch.Description)
            .WithColor(upcoming ? Color.Blue : success ? Color.Green : Color.Red);

        var img = launch.Images.Random(rng);
        if (img != null)
            builder.WithImageUrl(img);

        var site = launch.LaunchPad.Name;
        if (!string.IsNullOrWhiteSpace(site))
        {
            builder = builder.AddField("Launch Site", $"[{site}]({launch.LaunchPad.MapUrl})");
        }

        if (launch.DateUtc.HasValue)
        {
            var date = launch.DateUtc.Value.ToString("HH\\:mm UTC dd-MMM-yyyy");

            if (!launch.Upcoming)
            {
                builder.AddField("Launch Date", date);
            }
            else
            {
                builder = builder.AddField("Launch Date", date, true);
                builder = builder.AddField("T-", (launch.DateUtc.Value - DateTime.UtcNow).Humanize());
            }
        }

        return builder;
    }

    public static string Summary(this ILaunchInfo launch)
    {
        var date = DateString(launch.DateUtc, launch.DatePrecision);
        if (launch.DateUtc < DateTime.UtcNow)
            date = "";

        var site = launch.LaunchPad.Name;
        var name = launch.Name;
        var type = launch.Vehicle.Name;
        return $"- `{name}` from `{site}` on a {type} rocket {date}";
    }

    private static string DateString(DateTime? date, DatePrecision? precision)
    {
        if (!date.HasValue)
            return "";

        return precision switch
        {
            null => date.Value.Humanize(),
            DatePrecision.Minute => date.Value.Humanize(),
            DatePrecision.Hour => date.Value - DateTime.UtcNow > TimeSpan.FromDays(1) ? date.Value.Humanize() : $"about {date.Value.Humanize()}",
            DatePrecision.Day => $"on {date.Value:m}",
            DatePrecision.Month => $"in {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Value.Month)} {date.Value.Year}",
            DatePrecision.Quarter => $"in Q{date.Value.Month / 4 + 1} {date.Value.Year}",
            DatePrecision.Half => $"in H{date.Value.Month / 6 + 1} {date.Value.Year}",
            DatePrecision.Year => $"in {date.Value.Year}",
            _ => throw new NotSupportedException($"Tenatative date time `{precision.Value}`, unknown precision")
        };
    }
}