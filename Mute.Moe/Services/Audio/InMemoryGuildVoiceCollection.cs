using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Mute.Moe.Services.Audio;

public class InMemoryGuildVoiceCollection
    : IGuildVoiceCollection
{
    private readonly ConcurrentDictionary<ulong, IGuildVoice> _lookup = new();
    private readonly DiscordSocketClient _client;

    public InMemoryGuildVoiceCollection(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task<IGuildVoice> GetPlayer(ulong guild)
    {
        return _lookup.GetOrAdd(
            guild,
            static (t, args) => new ThreadedGuildVoice(args._client.GetGuild(args.guild), args._client),
            (guild, _client)
        );
    }
}