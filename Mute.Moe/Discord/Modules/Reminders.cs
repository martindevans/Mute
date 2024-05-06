using System.Globalization;
using System.Threading.Tasks;
using BalderHash;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Mute.Moe.Discord.Commands;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.Reminders;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
public class Reminders(IReminders _reminders)
    : BaseModule
{
    private static readonly Color Color = Color.Purple;

    private const string PastValueErrorMessage = "I'm sorry, but $moment$ is in the past.";

    [Command("remindme"), Alias("remind", "remind-me", "remind_me", "reminder"), Summary("I will remind you of something after a period of time")]
    [UsedImplicitly]
    public async Task CreateReminderCmd([Remainder] string message)
    {
        var msg = await CreateReminder(Context, message);
        await TypingReplyAsync(msg);
    }

    [Command("reminders"), Summary("I will give you a list of all your pending reminders")]
    [UsedImplicitly]
    public Task ListReminders()
    {
        return ListReminders(Context.User);
    }

    [Command("reminders"), Summary("I will give you a list of all pending reminders for a user"), RequireOwner]
    [UsedImplicitly]
    public async Task ListReminders(IUser user)
    {
        var items = await _reminders.Get(user.Id).ToArrayAsync();

        await DisplayItemList(
            items,
            async () => await ReplyAsync("No pending reminders"),
            async i => {
                await ReplyAsync("One pending reminder:");
                await DisplayReminder(i);
            },
            async l => await ReplyAsync($"{l.Count} pending reminders:"), (n, _) => DisplayReminder(n)
        );
    }

    private async Task DisplayReminder(IReminder reminder)
    {
        var embed = new EmbedBuilder()
            .WithColor(Color)
            .WithDescription(reminder.Message.Replace("`", "'"))
            .WithTimestamp(new DateTimeOffset(reminder.TriggerTime))
            .WithFooter(new BalderHash32(reminder.ID).ToString())
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("cancel-reminder"), Alias("reminder-cancel", "remind-cancel", "cancel-remind", "unremind"), Summary("I will delete a reminder with the given ID")]
    [UsedImplicitly]
    public async Task CancelReminder( string id)
    {
        var parsed = BalderHash32.Parse(id);
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

    private async Task<string> CreateReminder(ICommandContext context, string message)
    {
        var maybeTriggerMoment = message.TryParseReminderMoment();

        if (maybeTriggerMoment is not DateTime triggerMoment)
            return "That doesn't seem to be a valid moment.";
        if (triggerMoment < DateTime.UtcNow )
            return PastValueErrorMessage.Replace("$moment$", triggerMoment.ToString(CultureInfo.InvariantCulture));

        // Add some context to the message
        var prelude = $"{context.Message.Author.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst: triggerMoment, culture: CultureInfo.GetCultureInfo("en-gn"))}...";
        var msg = $"remind me {message}";

        // Save to database
        var n = await _reminders.Create(triggerMoment, prelude, msg, context.Message.Channel.Id, context.User.Id);

        var duration = triggerMoment - DateTime.UtcNow;
        var friendlyId = new BalderHash32(n.ID);
        return $"I will remind you in {duration.Humanize(2, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second, toWords: true)} (id: `{friendlyId}`)";
    }
}

public class RemindersCommandWord(IReminders _reminders)
    : ICommandWordHandler
{
    public IReadOnlyList<string> Triggers { get; } =
    [
        "remind", "Remind", "reminder", "Reminder"
    ];

    public async Task<bool> Invoke(MuteCommandContext context, string message)
    {
        var maybeTriggerMoment = message.TryParseReminderMoment();

        if (maybeTriggerMoment is not DateTime triggerMoment)
            return false;
        if (triggerMoment < DateTime.UtcNow)
            return false;

        // Add some context to the message
        var prelude = $"{context.Message.Author.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst: triggerMoment)}...";
        var msg = $"remind me {message}";

        // Save to database
        var n = await _reminders.Create(triggerMoment, prelude, msg, context.Message.Channel.Id, context.User.Id);

        // Send a response right now acknowledging it
        var duration = triggerMoment - DateTime.UtcNow;
        var friendlyId = new BalderHash32(n.ID);
        await context.Channel.SendMessageAsync($"Created a reminder {duration.Humanize(2, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second, toWords: true)} from now. (id: `{friendlyId}`)");

        return true;
    }
}