using System.Threading.Tasks;
using Discord.Interactions;

namespace Mute.Moe.Discord.Interactions
{
    public class Utility
        : InteractionModuleBase
    {
        [SlashCommand("ping", "Check that I am awake")]
        public async Task Ping()
        {
            await RespondAsync("pong");
        }
    }
}
