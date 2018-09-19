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

        [Command("remindme"), Alias("remind", "remind-me", "remind_me", "reminder")]
        public async Task CreateReminder([CanBeNull, Remainder] string message)
        {
            try
            {
                var result = ValidateAndExtract(message, Culture.EnglishOthers);

                string error = null;
                if (!result.IsValid)
                    error = result.ErrorMessage;
                else if (result.Value < DateTime.UtcNow)
                    error = PastValueErrorMessage;

                if (error != null)
                {
                    var msg = await this.TypingReplyAsync(result.ErrorMessage);
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
            Extraction Extract()
            {
                // Get DateTime for the specified culture
                var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

                //Try to get the date/time
                var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2"));
                if (dt == null)
                    return null;

                var resolutionValues = (IList<Dictionary<string, string>>)dt.Resolution["values"];

                //The time result could be one of several types
                var subType = dt.TypeName.Split('.').Last();

                //Check if it's a date/time, but not a range
                if (subType.Contains("date") && !subType.Contains("range"))
                {
                    var moment = resolutionValues.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                    return new Extraction {IsValid = true, Value = moment};
                }

                //Check if it's a range of times, in which case return the start of the range
                if (subType.Contains("date") && subType.Contains("range"))
                {
                    var from = DateTime.Parse(resolutionValues.First()["start"]);

                    return new Extraction {IsValid = true, Value = from};
                }

                //Check if it's just a plain time
                if (subType.Contains("time"))
                {
                    var values = resolutionValues.FirstOrDefault();
                    if (values == null)
                        return null;

                    if (!values.TryGetValue("value", out var value))
                        return null;

                    var moment = DateTime.Parse(value);

                    if (values.TryGetValue("utcOffsetMins", out var utcOff))
                    {
                        var offset = int.Parse(utcOff);
                        moment -= TimeSpan.FromMinutes(offset);
                    }

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
