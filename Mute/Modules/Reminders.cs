using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;

namespace Mute.Modules
{
    public class Reminders
        : ModuleBase
    {
        private readonly ReminderService _reminder;

        public Reminders(ReminderService reminder)
        {
            _reminder = reminder;
        }

        [Command("remindme"), Alias("remind", "remind-me", "remind_me", "reminder")]
        public async Task CreateReminder([CanBeNull] string time, [CanBeNull, Remainder] string message = null)
        {
            var parsed = time == null ? null : TryParse(time);
            if (!parsed.HasValue)
            {
                await this.TypingReplyAsync("I don't understand that time format. Use e.g. '3d2h4m12s'");
                return;
            }

            await this.TypingReplyAsync($"I will remind you in {parsed.Value.Humanize(2, maxUnit:TimeUnit.Year, minUnit:TimeUnit.Second, toWords: true)}");

            var triggerTime = DateTime.UtcNow + parsed.Value;

            //Add some context to the message
            message = $"{Context.Message.Author.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst:triggerTime)}: `{message}`";

            await _reminder.Create(triggerTime, message, Context.Message.Channel.Id);
        }

        private static TimeSpan? TryParse([NotNull] string str)
        {
            var match = Regex.Match(str, "((?<days>[0-9]+)d)?((?<hours>[0-9]+)h)?((?<mins>[0-9]+)m)?((?<secs>[0-9]+)s)?");

            if (!match.Success)
                return null;

            var days = match.Groups["days"];
            var hours = match.Groups["hours"];
            var mins = match.Groups["mins"];
            var secs = match.Groups["secs"];

            var result = TimeSpan.Zero;

            if (days.Success && days.Captures.Count > 0)
            {
                if (!int.TryParse(days.Captures[0].Value, out var d))
                    return null;
                result += TimeSpan.FromDays(d);
            }

            if (hours.Success && hours.Captures.Count > 0)
            {
                if (!int.TryParse(hours.Captures[0].Value, out var h))
                    return null;
                result += TimeSpan.FromHours(h);
            }

            if (mins.Success && mins.Captures.Count > 0)
            {
                if (!int.TryParse(mins.Captures[0].Value, out var m))
                    return null;
                result += TimeSpan.FromMinutes(m);
            }

            if (secs.Success && secs.Captures.Count > 0)
            {
                if (!int.TryParse(secs.Captures[0].Value, out var s))
                    return null;
                result += TimeSpan.FromSeconds(s);
            }

            return result;
        }
    }
}
