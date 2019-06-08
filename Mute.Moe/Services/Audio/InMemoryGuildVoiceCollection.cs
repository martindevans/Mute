using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.Audio
{
    public class InMemoryGuildVoiceCollection
        : IGuildVoiceCollection
    {
        private readonly ConcurrentDictionary<ulong, IGuildVoice> _lookup = new ConcurrentDictionary<ulong, IGuildVoice>();
        private readonly DiscordSocketClient _client;

        public InMemoryGuildVoiceCollection(DiscordSocketClient client)
        {
            _client = client;
        }

        public Task<IGuildVoice> GetPlayer(IGuild guild)
        {
            return Task.FromResult(_lookup.GetOrAdd(guild.Id, _ => new ThreadedGuildVoice(guild, _client)));
        }
    }
}
