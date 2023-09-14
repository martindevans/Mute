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

        if (precision is not > DatePrecision.Minute)
            return date.Value.Humanize();

        switch (precision.Value)
        {
            case DatePrecision.Hour:
                return date.Value - DateTime.UtcNow > TimeSpan.FromDays(1)
                     ? date.Value.Humanize()
                     : $"about {date.Value.Humanize()}";

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
                throw new NotSupportedException($"Tenatative date time `{precision.Value}`, unknown precision");
        }
    }
}