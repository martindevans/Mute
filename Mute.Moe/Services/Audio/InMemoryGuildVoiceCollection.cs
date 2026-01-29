using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Mute.Moe.Services.Audio;

/// <inheritdoc />
public class InMemoryGuildVoiceCollection
    : IGuildVoiceCollection
{
    private readonly ConcurrentDictionary<ulong, IGuildVoice> _lookup = new();
    private readonly DiscordSocketClient _client;

    /// <summary>
    /// Create new collection
    /// </summary>
    /// <param name="client"></param>
    public InMemoryGuildVoiceCollection(DiscordSocketClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<IGuildVoice> GetPlayer(ulong guild)
    {
        return _lookup.GetOrAdd(
            guild,
            static (guild, client) => new ThreadedGuildVoice(client.GetGuild(guild), client),
            _client
        );
    }
}