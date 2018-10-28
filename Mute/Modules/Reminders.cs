using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class Reminders
        : BaseModule, IKeyProvider
    {
        private static readonly Color Color = Color.Purple;

        private readonly ReminderService _reminder;

        private const string PastValueErrorMessage = "I'm sorry, but $moment$ is in the past.";
        private const string CannotParseErrorMessage = "That doesn't seem to be a valid date and time.";

        public Reminders(ReminderService reminder)
        {
            _reminder = reminder;
        }

        [Command("remindme"), Alias("remind", "remind-me", "remind_me", "reminder"), Summary("I will remind you of something after a period of time")]
        public async Task CreateReminderCmd([CanBeNull, Remainder] string message)
        {
            var msg = await CreateReminder(Context, message);
            if (msg != null)
                await TypingReplyAsync(msg);
        }

        [Command("reminders"), Summary("I will give you a list of all your pending reminders")]
        public async Task ListReminders()
        {
            await ListReminders(Context.User);
        }

        [Command("reminders"), Summary("I will give you a list of all pending reminders for a user"), RequireOwner]
        public async Task ListReminders([NotNull] IUser user)
        {
            var items = _reminder.Get(user.Id).OrderBy(a => a.TriggerTime).ToArray();

            await DisplayItemList(items,
                async () => await ReplyAsync("No pending reminders"),
                async i => {
                    await ReplyAsync("One pending reminder:");
                    await DisplayReminder(i);
                },
                async l => await ReplyAsync($"{l.Count} pending reminders:"),
                async (n, _) => await DisplayReminder(n)
            );
        }

        private async Task DisplayReminder([NotNull] ReminderService.Notification n)
        {
            var embed = new EmbedBuilder()
                 .WithColor(Color)
                 .WithDescription(n.Message.Replace("`", "'"))
                 .WithTimestamp(new DateTimeOffset(n.TriggerTime))
                 .WithFooter(n.UID)
                 .Build();

            await ReplyAsync("", false, embed);
        }

        [Command("cancel-reminder"), Alias("reminder-cancel", "remind-cancel", "cancel-remind", "unremind"), Summary("I will delete a reminder with the given ID")]
        public async Task CancelReminder([NotNull] string id)
        {
            if (await _reminder.Delete(id))
                await TypingReplyAsync($"Deleted reminder `{id}`");
            else
                await TypingReplyAsync($"I can't find a reminder with id `{id}`");
        }

        #region parsing
        [NotNull] private static Extraction ValidateAndExtract(string input, string culture)
        {
            TimeSpan ApplyTimezone(IReadOnlyDictionary<string, string> values)
            {
                if (values.TryGetValue("utcOffsetMins", out var utcOff))
                {
                    var offset = int.Parse(utcOff);
                    return -TimeSpan.FromMinutes(offset);
                }

                return TimeSpan.Zero;
            }

            Extraction Extract()
            {
                // Get DateTime for the specified culture
                var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

                //Try to get the date/time
                var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2"));
                if (dt == null)
                    return null;

                var resolutionValues = ((IList<Dictionary<string, string>>)dt.Resolution["values"])?.FirstOrDefault();
                if (resolutionValues == null)
                    return null;

                //The time result could be one of several types
                var subType = dt.TypeName.Split('.').Last();

                //Check if it's a date/time, but not a range
                if ((subType.Contains("date") && !subType.Contains("range")) || subType.Contains("time"))
                {
                    if (!resolutionValues.TryGetValue("value", out var value))
                        return null;

                    if (!DateTime.TryParse(value, out var moment))
                        return null;

                    moment += ApplyTimezone(resolutionValues);
                    return new Extraction { IsValid = true, Value = moment };
                }

                //Check if it's a range of times, in which case return the start of the range
                if (subType.Contains("date") && subType.Contains("range"))
                {
                    if (!resolutionValues.TryGetValue("start", out var value))
                        return null;

                    if (!DateTime.TryParse(value, out var moment))
                        return null;

                    moment += ApplyTimezone(resolutionValues);

                    return new Extraction {IsValid = true, Value = moment};
                }

                return null;
            }

            return Extract() ?? new Extraction
            {
                IsValid = false,
                Value = DateTime.MinValue,
                ErrorMessage = CannotParseErrorMessage
            };
        }

        private class Extraction
        {
            public bool IsValid { get; set; }

            public DateTime Value { get; set; }

            public string ErrorMessage { get; set; }
        }
        #endregion

        [ItemCanBeNull]
        private async Task<string> CreateReminder(ICommandContext context, string message)
        {
            try
            {
                var result = ValidateAndExtract(message, Culture.EnglishOthers);

                string error = null;
                if (!result.IsValid)
                    error = result.ErrorMessage;
                else if (result.Value < DateTime.UtcNow)
                    error = PastValueErrorMessage.Replace("$moment$", result.Value.ToString(CultureInfo.InvariantCulture));

                if (error != null)
                {
                    return error;
                }
                else
                {
                    var triggerTime = result.Value;
                    var duration = triggerTime - DateTime.UtcNow;

                    //Add some context to the message
                    var prelude = $"{context.Message.Author.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst: triggerTime, culture: CultureInfo.GetCultureInfo("en-gn"))}...";
                    var msg = $"remind me {message}";

                    //Save to database
                    var n = await _reminder.Create(triggerTime, prelude, msg, context.Message.Channel.Id, context.User.Id);

                    return $"I will remind you in {duration.Humanize(2, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second, toWords: true)} (id: `{n.UID}`)";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("remind", 10,
                    new Decomposition("remind me *", (c, d) => CreateReminder(c, d[0]))
                );
            }
        }
    }
}
