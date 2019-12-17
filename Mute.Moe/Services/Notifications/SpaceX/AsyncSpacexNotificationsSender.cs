using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Information.SpaceX;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Notifications.SpaceX
{
    public class AsyncSpacexNotificationsSender
        : ISpacexNotificationsSender
    {
        private readonly DiscordSocketClient _client;
        private readonly ISpacexNotifications _notifications;
        private readonly ISpacexInfo _spacex;

        private static readonly IReadOnlyList<TimeSpan> NotificationTimes = new[] {
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(6),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1)
        };

        public bool Status => !_thread.IsFaulted;

        private readonly Task _thread;
        private NotificationState _state;

        public AsyncSpacexNotificationsSender(DiscordSocketClient client, ISpacexNotifications notifications, ISpacexInfo spacex)
        {
            _client = client;
            _notifications = notifications;
            _spacex = spacex;

            _thread = Task.Run(ThreadEntry);
        }

        private async Task ThreadEntry()
        {
            try
            {
                // Initial setup
                while (true)
                {
                    var state = await _spacex.NextLaunch();
                    if (state == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        continue;
                    }

                    //Set up the initial state as if we just notified about this flight
                    _state = new NotificationState(state, DateTime.UtcNow);
                    break;
                }

                while (true)
                {
                    //Unconditionally wait 1 second so we don't hit the spacex API too often
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    //Get the next launch according to the API
                    var newNext = await _spacex.NextLaunch();
                    if (newNext == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    //There are 4 possibilities here:
                    // 1. Scrub, launch time of same flight has been pushed back
                    // 2. Scrub, launch time has been pushed back so far another flight is next
                    // 3. AOK, same flight number as last time is on track to launch
                    // 4. AOK, same flight number as last time is on track to launch, a notification needs to be sent

                    //If the actual launch ID has changed notify about that
                    if (newNext.FlightNumber != _state.Launch.FlightNumber)
                    {
                        var msg = NextLaunchChangedMessage(_state.Launch, newNext);
                        _state = await SendNotification(newNext, msg);
                        continue;
                    }

                    //If the expected launch time has changed, notify about that
                    if (newNext.LaunchDateUtc != _state.LaunchTimeUtc)
                    {
                        if (newNext.LaunchDateUtc.HasValue && _state.LaunchTimeUtc.HasValue)
                        {
                            var msg = ExpectedLaunchTimeChangedMessage(_state.Launch, _state.LaunchTimeUtc.Value, newNext.LaunchDateUtc.Value);
                            _state = await SendNotification(newNext, msg);
                            continue;
                        }
                    }

                    //Check if we need to send one of the time reminders for this launch
                    if (newNext.LaunchDateUtc.HasValue)
                    {
                        var timeToLaunch = newNext.LaunchDateUtc.Value - DateTime.UtcNow;
                        var notificationNow = NotificationTimes.Where(nt => nt > timeToLaunch && nt < _state.TimeToLaunch).Cast<TimeSpan?>().SingleOrDefault();
                        var nextNotification = NotificationTimes.Where(nt => nt < timeToLaunch).Cast<TimeSpan?>().FirstOrDefault() ?? TimeSpan.FromSeconds(30);

                        //If we have passed a threshold, send a notification now
                        if (notificationNow.HasValue)
                        {
                            var msg = PeriodicReminderMessage(newNext);
                            _state = await SendNotification(newNext, msg);
                        }

                        //Wait for the next notification, or an hour, whichever is less
                        var nextEventTime = timeToLaunch - nextNotification;
                        if (nextEventTime < TimeSpan.FromHours(1))
                        {
                            if (nextEventTime > TimeSpan.FromSeconds(0.5f))
                                await Task.Delay(nextEventTime);
                            else
                                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                        }
                        else
                            await Task.Delay(TimeSpan.FromHours(1));
                    }
                    else
                        await Task.Delay(TimeSpan.FromHours(1));
                }
            }
            catch (Exception e)
            {
                var info = await _client.GetApplicationInfoAsync();
                if (info.Owner != null)
                {
                    var channel = await info.Owner.GetOrCreateDMChannelAsync();
                    await channel.SendMessageAsync($"{nameof(AsyncSpacexNotificationsSender)} notifications thread crashed:");
                    await channel.SendLongMessageAsync(e.ToString());
                }
            }
        }

        [NotNull] private static string NextLaunchChangedMessage([NotNull] LaunchInfo previous, [NotNull] LaunchInfo next)
        {
            return $"The next SpaceX Launch has changed from {previous.MissionName} to {next.MissionName}";
        }

        [NotNull] private static string ExpectedLaunchTimeChangedMessage([NotNull] LaunchInfo launch, DateTime previousT, DateTime newT)
        {
            var delay = newT - previousT;
            return $"SpaceX launch {launch.MissionName} has been delayed by {delay.Humanize()} to {newT:HH\\:mm UTC dd-MMM-yyyy}";
        }

        [NotNull] private static string PeriodicReminderMessage([NotNull] LaunchInfo launch)
        {
            //Append video link if there is one.
            var video = "";
            if (launch.Links.VideoLink != null)
                video = $". Watch it here: {launch.Links.VideoLink}.";

            return $"SpaceX launch {launch.MissionName} will launch in {launch.LaunchDateUtc.Humanize()} {video}";
        }

        [NotNull, ItemNotNull]
        private async Task<NotificationState> SendNotification([NotNull] LaunchInfo launch, string message)
        {
            var subs = await _notifications.GetSubscriptions();
            await subs.EnumerateAsync(async s =>
            {
                if (!(_client.GetChannel(s.Channel) is ITextChannel channel))
                    return;

                var m = message;
                if (s.MentionRole.HasValue && channel is IGuildChannel gc)
                {
                    var role = gc.Guild.GetRole(s.MentionRole.Value);
                    if (role != null)
                        m = $"{role.Mention} {message}";
                }

                await channel.SendMessageAsync(m);
            });

            return new NotificationState(launch, DateTime.UtcNow);
        }

        private class NotificationState
        {
            public LaunchInfo Launch { get; }
            public TimeSpan? TimeToLaunch { get; }
            public DateTime? LaunchTimeUtc { get; }

            public NotificationState([NotNull] LaunchInfo launch, DateTime utcNow)
            {
                Launch = launch;

                TimeToLaunch = launch.LaunchDateUtc - utcNow;
                LaunchTimeUtc = launch.LaunchDateUtc;
            }
        }
    }
}
