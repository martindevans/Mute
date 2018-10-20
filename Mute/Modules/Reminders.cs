using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace Mute.Modules
{
    public class Reminders
        : ModuleBase
    {
        private readonly ReminderService _reminder;
        private readonly Random _random;

        private const string PastValueErrorMessage = "I'm sorry, but $moment$ is in the past.";
        private const string CannotParseErrorMessage = "That doesn't seem to be a valid date and time.";

        public Reminders(ReminderService reminder, Random random)
        {
            _reminder = reminder;
            _random = random;
        }

        [Command("remindme"), Alias("remind", "remind-me", "remind_me", "reminder"), Summary("I will remind you of something after a period of time")]
        public async Task CreateReminder([CanBeNull, Remainder] string message)
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
                    var msg = await this.TypingReplyAsync(error);
                    if (_random.NextDouble() < 0.05f)
                        await msg.AddReactionAsync(EmojiLookup.Confused);
                }
                else
                {

                    var triggerTime = result.Value;
                    var duration = triggerTime - DateTime.UtcNow;

                    await this.TypingReplyAsync($"I will remind you in {duration.Humanize(2, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second, toWords: true)}");

                    //Add some context to the message
                    message = $"{Context.Message.Author.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst: triggerTime, culture: CultureInfo.GetCultureInfo("en-gn"))}: `remind me {message}`";

                    await _reminder.Create(triggerTime, message, Context.Message.Channel.Id);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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
    }
}
