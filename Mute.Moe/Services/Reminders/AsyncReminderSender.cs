using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalderHash;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.Reminders
{
    public class AsyncReminderSender
        : IReminderSender
    {
        private readonly IReminders _reminders;
        private readonly DiscordSocketClient _client;

        private CancellationTokenSource? _cts;
        private Task? _thread;

        public TaskStatus Status => _thread?.Status ?? TaskStatus.WaitingForActivation;

        public AsyncReminderSender(IReminders reminders, DiscordSocketClient client)
        {
            _reminders = reminders;
            _client = client;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _thread = Task.Run(() => ThreadEntry(_cts.Token), _cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
        }

        private async Task ThreadEntry(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    //Get the first unsent reminder
                    // ReSharper disable once RedundantCast
                    var next = (IReminder?)await _reminders.Get(count: 1).FirstOrDefaultAsync();

                    //Wait for one of these events to happen
                    var cts = new CancellationTokenSource();
                    var evt = await await Task.WhenAny(
                        WaitForCreation(cts.Token),
                        WaitForDeletion(cts.Token),
                        WaitForTimeout(cts.Token, next)
                    );

                    //cancel all the others
                    cts.Cancel();

                    //Run whichever one completed
                    await evt.Run(ref next);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Notification service killed! Exception: {0}", e);
            }
        }

        private async Task<BaseEventAction> WaitForCreation(CancellationToken ct)
        {
            //Create a task which will complete when a new reminder is created
            var tcs = new TaskCompletionSource<IReminder>();
            _reminders.ReminderCreated += tcs.SetResult;

            //If the wait task is cancelled cancel the inner task and unregister the event handler
            ct.Register(() => tcs.TrySetCanceled());
            ct.Register(() => _reminders.ReminderCreated -= tcs.SetResult);

            //Now wait for something to happen...
            var reminder = await tcs.Task;

            //If something happened, return the reminder
            return new EventCreatedAction(reminder);
        }

        private async Task<BaseEventAction> WaitForDeletion(CancellationToken ct)
        {
            //Create a task which will complete when a reminder is deleted
            var tcs = new TaskCompletionSource<uint>();
            _reminders.ReminderDeleted += tcs.SetResult;

            //If the wait task is cancelled cancel the inner task and unregister the event handler
            ct.Register(() => tcs.TrySetCanceled());
            ct.Register(() => _reminders.ReminderDeleted -= tcs.SetResult);

            //Now wait for something to happen...
            var id = await tcs.Task;

            //If something happened, return the reminder
            return new EventDeletedAction(id);
        }

        private async Task<BaseEventAction> WaitForTimeout(CancellationToken ct, IReminder? next)
        {
            //If there is no next event then just hang forever
            if (next == null)
            {
                await Task.Delay(-1, ct);
                return await Task.FromCanceled<BaseEventAction>(ct);
            }
            else
            {
                //Wait for this reminder to time out
                while (next.TriggerTime > DateTime.UtcNow)
                {
                    var delay = Math.Clamp((int)(next.TriggerTime - DateTime.UtcNow).TotalMilliseconds, 1, 3600000);
                    await Task.Delay(delay, ct);
                }

                return new EventTimeoutAction(next, _reminders, _client);
            }
        }

        private abstract class BaseEventAction
        {
             public abstract Task Run(ref IReminder? next);
        }

        private class EventCreatedAction
            : BaseEventAction
        {
            private readonly IReminder _reminder;

            public EventCreatedAction( IReminder reminder)
            {
                _reminder = reminder;
            }

            public override Task Run(ref IReminder? next)
            {
                Console.WriteLine("Create reminder " + _reminder.ID);

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

            public override Task Run(ref IReminder? next)
            {
                Console.WriteLine("Delete reminder " + _id);

                if (_id == next?.ID)
                    next = null;
                return Task.CompletedTask;
            }
        }

        private class EventTimeoutAction
            : BaseEventAction
        {
            private readonly IReminder _reminder;
            private readonly IReminders _reminders;
            private readonly DiscordSocketClient _client;

            public EventTimeoutAction(IReminder reminder, IReminders reminders, DiscordSocketClient client)
            {
                _reminder = reminder;
                _reminders = reminders;
                _client = client;
            }

            public override Task Run(ref IReminder? _)
            {
                Console.WriteLine("Send reminder " + _reminder.ID);

                return Task.Run(async () =>
                {
                    if (_client.GetChannel(_reminder.ChannelId) is ITextChannel channel)
                    {
                        if (!string.IsNullOrWhiteSpace(_reminder.Prelude))
                            await channel.SendMessageAsync(_reminder.Prelude);

                        string name = await Name(_reminder.UserId, channel.Guild);

                        var embed = new EmbedBuilder()
                            .WithDescription(_reminder.Message)
                            .WithAuthor(name)
                            .WithFooter(new BalderHash32(_reminder.ID).ToString());

                        await channel.SendMessageAsync(embed: embed.Build());
                    }
                    else
                    {
                        Console.WriteLine($"Cannot send reminder: Channel `{_reminder.ChannelId}` is null");
                    }

                    await _reminders.Delete(_reminder.UserId, _reminder.ID);
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
}
