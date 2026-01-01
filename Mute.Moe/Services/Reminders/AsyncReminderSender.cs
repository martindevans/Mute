using BalderHash;
using Discord;
using Discord.WebSocket;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Reminders;

/// <inheritdoc />
public class AsyncReminderSender(IReminders _reminders, DiscordSocketClient _client)
    : IReminderSender
{
    private CancellationTokenSource? _cts;
    private Task? _thread;

    /// <summary>
    /// Status of the notification sender thread
    /// </summary>
    public TaskStatus Status => _thread?.Status ?? TaskStatus.WaitingForActivation;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _thread = Task.Run(() => ThreadEntry(_cts.Token), _cts.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
    }

    private async Task ThreadEntry(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                //Get the first unsent reminder
                // ReSharper disable once RedundantCast
                var next = await _reminders.Get(count: 1).FirstOrDefaultAsync(cancellationToken: token);

                //Wait for one of these events to happen
                var cts = new CancellationTokenSource();
                var evt = await await Task.WhenAny(
                    WaitForCreation(cts.Token),
                    WaitForDeletion(cts.Token),
                    WaitForTimeout(cts.Token, next)
                );

                //cancel all the others
                await cts.CancelAsync();

                //Run whichever one completed
                await evt.Run(ref next);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception killed {nameof(AsyncReminderSender)} thread");
        }
    }

    private async Task<BaseEventAction> WaitForCreation(CancellationToken ct)
    {
        // Create a task which will complete when a new reminder is created
        var tcs = new TaskCompletionSource<Reminder>();
        _reminders.ReminderCreated += tcs.SetResult;

        // If the wait task is cancelled cancel the inner task and unregister the event handler
        ct.Register(() => tcs.TrySetCanceled());
        ct.Register(() => _reminders.ReminderCreated -= tcs.SetResult);

        // Now wait for something to happen...
        var reminder = await tcs.Task;

        // If something happened, return the reminder
        return new EventCreatedAction(reminder);
    }

    private async Task<BaseEventAction> WaitForDeletion(CancellationToken ct)
    {
        // Create a task which will complete when a reminder is deleted
        var tcs = new TaskCompletionSource<uint>();
        _reminders.ReminderDeleted += tcs.SetResult;

        // If the wait task is cancelled cancel the inner task and unregister the event handler
        ct.Register(() => tcs.TrySetCanceled());
        ct.Register(() => _reminders.ReminderDeleted -= tcs.SetResult);

        // Now wait for something to happen...
        var id = await tcs.Task;

        // If something happened, return the reminder
        return new EventDeletedAction(id);
    }

    private async Task<BaseEventAction> WaitForTimeout(CancellationToken ct, Reminder? next)
    {
        // If there is no next event then just hang forever
        if (next == null)
        {
            await Task.Delay(-1, ct);
            return await Task.FromCanceled<BaseEventAction>(ct);
        }

        // Wait for this reminder to time out
        while (next.TriggerTime > DateTime.UtcNow)
        {
            var delay = Math.Clamp((int)(next.TriggerTime - DateTime.UtcNow).TotalMilliseconds, 1, 3600000);
            await Task.Delay(delay, ct);
        }

        return new EventTimeoutAction(next, _reminders, _client);
    }

    private abstract class BaseEventAction
    {
        public abstract Task Run(ref Reminder? next);
    }

    private class EventCreatedAction
        : BaseEventAction
    {
        private readonly Reminder _reminder;

        public EventCreatedAction(Reminder reminder)
        {
            _reminder = reminder;
        }

        public override Task Run(ref Reminder? next)
        {
            Log.Information("Created reminder: {0}", _reminder.ID);

            if (next == null || _reminder.TriggerTime < next.TriggerTime)
                next = _reminder;

            return Task.CompletedTask;
        }
    }

    private class EventDeletedAction
        : BaseEventAction
    {
        private readonly uint _id;

        public EventDeletedAction(uint id)
        {
            _id = id;
        }

        public override Task Run(ref Reminder? next)
        {
            Log.Information("Deleted reminder: {0}", _id);

            if (_id == next?.ID)
                next = null;
            return Task.CompletedTask;
        }
    }

    private class EventTimeoutAction
        : BaseEventAction
    {
        private readonly Reminder _reminder;
        private readonly IReminders _reminders;
        private readonly DiscordSocketClient _client;

        public EventTimeoutAction(Reminder reminder, IReminders reminders, DiscordSocketClient client)
        {
            _reminder = reminder;
            _reminders = reminders;
            _client = client;
        }

        public override Task Run(ref Reminder? _)
        {
            Log.Information("Sending reminder: {0}", _reminder.ID);

            return Task.Run(async () =>
            {
                try
                {
                    if (_client.GetChannel(_reminder.ChannelId) is ITextChannel channel)
                    {
                        if (!string.IsNullOrWhiteSpace(_reminder.Prelude))
                            await channel.SendMessageAsync(_reminder.Prelude);

                        var name = await Name(_reminder.UserId, channel.Guild);

                        var embed = new EmbedBuilder()
                                   .WithDescription(_reminder.Message)
                                   .WithAuthor(name)
                                   .WithFooter(new BalderHash32(_reminder.ID).ToString());


                        await channel.SendMessageAsync(embed: embed.Build());
                    }
                    else
                    {
                        Log.Warning("Sending reminder {0}, but channel {1} is null", _reminder.ID, _reminder.ChannelId);

                        var user = await _client.GetUserAsync(_reminder.UserId);
                        if (user != null)
                        {
                            var embed = new EmbedBuilder()
                                       .WithDescription(_reminder.Message)
                                       .WithAuthor(user.GlobalName ?? user.Username)
                                       .WithFooter(new BalderHash32(_reminder.ID).ToString());

                            await user.SendMessageAsync(
                                $"You previously scheduled this reminder in channel '{_reminder.ChannelId}', but that channel no longer seems to exist!",
                                embed: embed.Build()
                            );
                        }
                        else
                        {
                            Log.Warning("Failed to send reminder {0}! Channel {1} and user {2} are both null", _reminder.ID, _reminder.ChannelId, _reminder.UserId);
                        }
                    }

                    await _reminders.Delete(_reminder.UserId, _reminder.ID);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to send reminder {0}", _reminder.ID);
                }
            });
        }

        private async Task<string> Name(ulong id, IGuild? guild)
        {
            if (guild != null)
            {
                var guildUser = (IGuildUser?)await guild.GetUserAsync(_reminder.UserId);
                var n = guildUser?.Nickname ?? guildUser?.Username;
                if (n != null)
                    return n;
            }
                
            var user = await _client.Rest.GetUserAsync(_reminder.UserId);
            if (user != null)
                return user.Username;

            return $"User{id}";
        }
    }
}