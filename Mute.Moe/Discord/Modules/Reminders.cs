using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Discord.Services.Responses.Eliza;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    public class Reminders
        : BaseModule, IKeyProvider
    {
        private static readonly Color Color = Color.Purple;

        private readonly IReminders _reminders;

        private const string PastValueErrorMessage = "I'm sorry, but $moment$ is in the past.";

        public Reminders(IReminders reminders)
        {
            _reminders = reminders;
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
            var items = await _reminders.Get(user.Id).ToArray();

            await DisplayItemList(
                items,
                async () => await ReplyAsync("No pending reminders"),
                async i => {
                    await ReplyAsync("One pending reminder:");
                    await DisplayReminder(i);
                },
                async l => await ReplyAsync($"{l.Count} pending reminders:"),
                async (n, _) => await DisplayReminder(n)
            );
        }

        private async Task DisplayReminder([NotNull] IReminder reminder)
        {
            var embed = new EmbedBuilder()
                 .WithColor(Color)
                 .WithDescription(reminder.Message.Replace("`", "'"))
                 .WithTimestamp(new DateTimeOffset(reminder.TriggerTime))
                 .WithFooter(new FriendlyId32(reminder.ID).ToString())
                 .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("cancel-reminder"), Alias("reminder-cancel", "remind-cancel", "cancel-remind", "unremind"), Summary("I will delete a reminder with the given ID")]
        public async Task CancelReminder([NotNull] string id)
        {
            var parsed = FriendlyId32.Parse(id);
            if (!parsed.HasValue)
            {
                await TypingReplyAsync("Invalid ID");
                return;
            }

            if (await _reminders.Delete(Context.User.Id, parsed.Value.Value))
                await TypingReplyAsync($"Deleted reminder `{id}`");
            else
                await TypingReplyAsync($"I can't find a reminder with id `{id}`");
        }

        [ItemCanBeNull]
        private async Task<string> CreateReminder(ICommandContext context, string message)
        {

            var result = FuzzyParsing.Moment(message);

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
                var n = await _reminders.Create(triggerTime, prelude, msg, context.Message.Channel.Id, context.User.Id);

                var friendlyId = new FriendlyId32(n.ID);
                return $"I will remind you in {duration.Humanize(2, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second, toWords: true)} (id: `{friendlyId}`)";
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
