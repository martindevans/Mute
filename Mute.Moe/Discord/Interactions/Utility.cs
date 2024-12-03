using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Interactions;

[UsedImplicitly]
public class Utility
    : MuteInteractionModuleBase
{
    private readonly BaseSocketClient _client;

    public Utility(BaseSocketClient client)
    {
        _client = client;
    }

    [SlashCommand("ping", "Check that I am awake")]
    [UsedImplicitly]
    public async Task Ping()
    {
        await RespondAsync("pong");
    }
}