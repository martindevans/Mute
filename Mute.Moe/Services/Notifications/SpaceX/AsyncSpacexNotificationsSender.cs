//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Discord;
//using Discord.WebSocket;
//using Humanizer;
//using Mute.Moe.Extensions;
//using Mute.Moe.Services.Information.SpaceX;
//using Oddity.Models.Launches;

//namespace Mute.Moe.Services.Notifications.SpaceX;

//public class AsyncSpacexNotificationsSender
//    : ISpacexNotificationsSender
//{
//    private readonly DiscordSocketClient _client;
//    private readonly ISpacexNotifications _notifications;
//    private readonly ISpacexInfo _spacex;

//    private static readonly IReadOnlyList<TimeSpan> NotificationTimes = new[] {
//        TimeSpan.FromDays(3),
//        TimeSpan.FromHours(2),
//        TimeSpan.FromMinutes(5),
//    };

//    private CancellationTokenSource? _cts;

//    private NotificationState? _state;

//    public AsyncSpacexNotificationsSender(DiscordSocketClient client, ISpacexNotifications notifications, ISpacexInfo spacex)
//    {
//        _client = client;
//        _notifications = notifications;
//        _spacex = spacex;
//    }

//    public async Task StartAsync(CancellationToken cancellationToken)
//    {
//        _cts = new CancellationTokenSource();
//        var _ = Task.Run(() => ThreadEntry(_cts.Token), _cts.Token);
//    }

//    public async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _cts?.Cancel();
//    }

//    private async Task ThreadEntry(CancellationToken token)
//    {
//        try
//        {
//            // Initial setup
//            while (!token.IsCancellationRequested)
//            {
//                var state = await _spacex.NextLaunch();
//                if (state == null)
//                {
//                    await Task.Delay(TimeSpan.FromSeconds(30), token);
//                }
//                else
//                {
//                    //Set up the initial state as if we just notified about this flight
//                    _state = new NotificationState(state, DateTime.UtcNow);
//                    break;
//                }
//            }

//            while (true)
//            {
//                //Unconditionally wait 1 second so we don't hit the spacex API too often
//                await Task.Delay(TimeSpan.FromSeconds(1), token);

//                //Get the next launch according to the API
//                var newNext = await _spacex.NextLaunch();
//                if (newNext == null)
//                {
//                    await Task.Delay(TimeSpan.FromSeconds(1), token);
//                    continue;
//                }

//                if (_state == null)
//                {
//                    _state = new NotificationState(newNext, DateTime.UtcNow);
//                    continue;
//                }

//                //There are 4 possibilities here:
//                // 1. Scrub, launch time of same flight has been pushed back
//                // 2. Scrub, launch time has been pushed back so far another flight is next
//                // 3. AOK, same flight number as last time is on track to launch
//                // 4. AOK, same flight number as last time is on track to launch, a notification needs to be sent

//                // If the actual launch ID has changed notify about that
//                if (newNext.FlightNumber != _state.Launch.FlightNumber)
//                {
//                    var msg = NextLaunchChangedMessage(_state.Launch, newNext);
//                    _state = await SendNotification(newNext, msg);
//                    continue;
//                }

//                // If the expected launch time has changed, notify about that
//                if (newNext.DateUtc != _state.LaunchTimeUtc)
//                {
//                    if (newNext.DateUtc != null && _state.LaunchTimeUtc != null)
//                    {
//                        var msg = ExpectedLaunchTimeChangedMessage(_state.Launch, _state.LaunchTimeUtc.Value, newNext.DateUtc.Value);
//                        _state = await SendNotification(newNext, msg);
//                        continue;
//                    }
//                }

//                //Check if we need to send one of the time reminders for this launch
//                if (newNext.DateUtc != null)
//                {
//                    var timeToLaunch = newNext.DateUtc.Value - DateTime.UtcNow;
//                    var notificationNow = NotificationTimes.Where(nt => nt > timeToLaunch && nt < _state.TimeToLaunch).Cast<TimeSpan?>().SingleOrDefault();
//                    var nextNotification = NotificationTimes.Where(nt => nt < timeToLaunch).Cast<TimeSpan?>().FirstOrDefault() ?? TimeSpan.FromSeconds(30);

//                    //If we have passed a threshold, send a notification now
//                    if (notificationNow.HasValue)
//                    {
//                        var msg = PeriodicReminderMessage(newNext);
//                        _state = await SendNotification(newNext, msg);
//                    }

//                    //Wait for the next notification, or an hour, whichever is less
//                    var nextEventTime = timeToLaunch - nextNotification;
//                    if (nextEventTime < TimeSpan.FromHours(1))
//                    {
//                        if (nextEventTime > TimeSpan.FromSeconds(0.5f))
//                            await Task.Delay(nextEventTime, token);
//                        else
//                            await Task.Delay(TimeSpan.FromSeconds(0.5f), token);
//                    }
//                    else
//                        await Task.Delay(TimeSpan.FromHours(1), token);
//                }
//                else
//                    await Task.Delay(TimeSpan.FromHours(1), token);
//            }
//        }
//        catch (Exception e)
//        {
//            var info = await _client.GetApplicationInfoAsync();
//            if (info.Owner != null)
//            {
//                var channel = await info.Owner.CreateDMChannelAsync();
//                await channel.SendMessageAsync($"{nameof(AsyncSpacexNotificationsSender)} notifications thread crashed:");
//                await channel.SendLongMessageAsync(e.ToString());
//            }
//        }
//    }

//    private static string NextLaunchChangedMessage(LaunchInfo previous, LaunchInfo next)
//    {
//        return $"The next SpaceX Launch has changed from {previous.FlightNumber}.{previous.Name} to {next.FlightNumber}.{next.Name}";
//    }

//    private static string ExpectedLaunchTimeChangedMessage(LaunchInfo launch, DateTime previousT, DateTime newT)
//    {
//        var delay = newT - previousT;
//        return $"SpaceX launch {launch.Name} has been delayed by {delay.Humanize()} to {newT:HH\\:mm UTC dd-MMM-yyyy}";
//    }

//    private static string PeriodicReminderMessage(LaunchInfo launch)
//    {
//        //Append video link if there is one.
//        var video = "";
//        if (launch.Links.Webcast != null)
//            video = $". Watch it here: {launch.Links.Webcast}.";

//        return $"SpaceX launch {launch.Name} will launch in {launch.DateUtc.Humanize()} {video}";
//    }

//    private async Task<NotificationState> SendNotification(LaunchInfo launch, string message)
//    {
//        var subs = _notifications.GetSubscriptions();

//        await foreach (var s in subs)
//        {
//            if (_client.GetChannel(s.Channel) is not ITextChannel channel)
//                continue;

//            var m = message;
//            if (s.MentionRole.HasValue && channel is IGuildChannel gc)
//            {
//                var role = gc.Guild.GetRole(s.MentionRole.Value);
//                if (role != null)
//                    m = $"{role.Mention} {message}";
//            }

//            await channel.SendMessageAsync(m);
//        }

//        return new NotificationState(launch, DateTime.UtcNow);
//    }

//    private class NotificationState
//    {
//        public LaunchInfo Launch { get; }
//        public TimeSpan? TimeToLaunch { get; }
//        public DateTime? LaunchTimeUtc { get; }

//        public NotificationState(LaunchInfo launch, DateTime utcNow)
//        {
//            Launch = launch;

//            TimeToLaunch = launch.DateUtc - utcNow;
//            LaunchTimeUtc = launch.DateUtc;
//        }
//    }
//}