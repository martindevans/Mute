using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
public class Time
    : BaseModule
{
    [Command("time"), Summary("I will tell you the time"), UsedImplicitly]
    public async Task TimeAsync([Remainder] string? tz = null)
    {
        await TypingReplyAsync(GetTime(tz));
    }

    private static string GetTime(string? tz = null)
    {
        var extract = FuzzyParsing.TimeOffset(tz ?? "");
        var offset = extract.IsValid ? extract.UtcOffset : TimeSpan.Zero;

        static string FormatTime(DateTime dt) => dt.ToString("HH:mm:ss tt");

        if (extract.IsValid || tz == null)
            return $"The time is {FormatTime(DateTime.UtcNow + offset)} UTC{offset.Hours:+00;-00;+00}:{offset.Minutes:00}";
        return $"I'm not sure what timezone you mean, assuming UTC it's {FormatTime(DateTime.UtcNow)}";
    }
}