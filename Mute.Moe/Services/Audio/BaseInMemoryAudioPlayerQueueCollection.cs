using System.Collections.Generic;
using System.Threading.Tasks;
using Mute.Moe.Services.Audio.Mixing.Channels;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Audio
{
    public abstract class BaseInMemoryAudioPlayerQueueCollection<TValue, TInterface>
        where TValue : IMixerChannel, TInterface
    {
        private readonly IGuildVoiceCollection _voice;

        private readonly AsyncLock _lookupLock = new AsyncLock();
        private readonly Dictionary<ulong, TValue> _lookup = new Dictionary<ulong, TValue>();

        protected BaseInMemoryAudioPlayerQueueCollection(IGuildVoiceCollection voice)
        {
            _voice = voice;
        }

        public async Task<TInterface> Get(ulong guild)
        {
            using (await _lookupLock.LockAsync())
            {
                if (!_lookup.TryGetValue(guild, out var value))
                {
                    var player = await _voice.GetPlayer(guild);
                    value = Create(player);
                    _lookup.Add(guild, value);
                    player.Open(value);
                }
                return value;
            }
        }

        protected abstract TValue Create(IGuildVoice voice);
    }
}
