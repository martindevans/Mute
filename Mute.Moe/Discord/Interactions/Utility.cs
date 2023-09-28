using System.Threading.Tasks;
using Discord.Interactions;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Interactions;

[UsedImplicitly]
public class Utility
    : MuteInteractionModuleBase
{
    [SlashCommand("ping", "Check that I am awake")]
    public async Task Ping()
    {
        await RespondAsync("pong");
    }
}