using System.Threading.Tasks;
using Discord.Interactions;

namespace Mute.Moe.Discord.Interactions;

/// <summary>
/// Basic utility slash commands
/// </summary>
[UsedImplicitly]
public class Utility
    : MuteInteractionModuleBase
{
    /// <summary>
    /// Simply check if the bot is alive
    /// </summary>
    /// <returns></returns>
    [SlashCommand("ping", "Check that I am awake")]
    [UsedImplicitly]
    public async Task Ping()
    {
        await RespondAsync("pong");
    }
}