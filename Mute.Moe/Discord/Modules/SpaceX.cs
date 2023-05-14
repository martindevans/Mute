using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Services.Information.SpaceX;
using Mute.Moe.Services.Information.SpaceX.Extensions;
using Mute.Moe.Services.Notifications.SpaceX;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("spacex")]
public class SpaceX
    : BaseModule
{
    private readonly ISpacexInfo _spacex;
    private readonly ISpacexNotifications _notifications;
    private readonly Random _rng;

    public SpaceX(ISpacexInfo spacex, ISpacexNotifications notifications, Random rng)
    {
        _spacex = spacex;
        _notifications = notifications;
        _rng = rng;
    }

    //[Command("details"), Alias("flight-no", "flight-num", "mission", "flight"), Summary("I will tell you about a specific spacex launch")]
    //public async Task LaunchDetails(uint id)
    //{
    //    var launch = await _spacex.Launch(id);
    //    if (launch == null)
    //    {
    //        await TypingReplyAsync("There doesn't seem to be a flight by that ID");
    //        return;
    //    }

    //    await ReplyAsync(launch.DiscordEmbed(_rng));
    //}

    [Command("next"), Alias("upcoming"), Summary("I will tell you about the next spacex launch(es)")]
    public async Task NextLaunches(int count = 1)
    {
        if (count == 1)
        {
            await ReplyAsync(await _spacex.NextLaunch().DiscordEmbed(_rng));
        }
        else
        {
            var launches = (await _spacex.Upcoming(count)).Where(a => a.DateUtc.HasValue).OrderBy(a => a.MissionNumber).Take(count).ToArray();
            await DisplayItemList(
                launches,
                () => "There are no upcoming SpaceX launches!",
                null,
                (l, _) => l.Summary()
            );
        }
    }

    [Command("subscribe"), RequireOwner]
    public async Task Subscribe(IRole? role = null)
    {
        await _notifications.Subscribe(Context.Channel.Id, role?.Id);
        await TypingReplyAsync("Subscribed to receive SpaceX mission updates");
    }
}