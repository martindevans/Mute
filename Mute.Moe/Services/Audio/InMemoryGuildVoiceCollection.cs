using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Mute.Moe.Services.Audio
{
    public class InMemoryGuildVoiceCollection
        : IGuildVoiceCollection
    {
        private readonly ConcurrentDictionary<ulong, IGuildVoice> _lookup = new();
        private readonly DiscordSocketClient _client;

        public InMemoryGuildVoiceCollection(DiscordSocketClient client)
        {
            _client = client;
        }

        public Task<IGuildVoice> GetPlayer(ulong guild)
        {
            return Task.FromResult(_lookup.GetOrAdd(guild, _ => new ThreadedGuildVoice(_client.GetGuild(guild), _client)));
        }
    }
}
