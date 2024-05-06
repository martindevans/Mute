using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Services.Information.SpaceX;
using Mute.Moe.Services.Information.SpaceX.Extensions;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("spacex")]
public class SpaceX(ISpacexInfo spacex, Random rng)
    : BaseModule
{
    [Command("next"), Alias("upcoming"), Summary("I will tell you about the next spacex launch(es)")]
    [UsedImplicitly]
    public async Task NextLaunches(int count = 1)
    {
        if (count == 1)
        {
            await ReplyAsync(await spacex.NextLaunch().DiscordEmbed(rng));
        }
        else
        {
            var launches = (await spacex.Upcoming(count)).Where(a => a.DateUtc.HasValue).OrderBy(a => a.MissionNumber).Take(count).ToArray();
            await DisplayItemList(
                launches,
                () => "There are no upcoming SpaceX launches!",
                null,
                (l, _) => l.Summary()
            );
        }
    }
}