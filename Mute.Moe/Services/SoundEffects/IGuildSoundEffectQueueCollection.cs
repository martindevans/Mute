using System.Threading.Tasks;

using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.SoundEffects
{
    public interface IGuildSoundEffectQueueCollection
    {
        Task<IGuildSoundEffectQueue> Get(ulong guild);
    }

    public interface IGuildSoundEffectQueue
        : ISimpleQueueChannel<ISoundEffect>
    {
        IGuildVoice VoicePlayer { get; }
    }

    public class InMemoryGuildSoundEffectQueueCollection
        : BaseInMemoryAudioPlayerQueueCollection<InMemoryGuildSoundEffectQueue, IGuildSoundEffectQueue>, IGuildSoundEffectQueueCollection
    {
        public InMemoryGuildSoundEffectQueueCollection(IGuildVoiceCollection voice)
            : base(voice)
        {
        }

         protected override InMemoryGuildSoundEffectQueue Create(IGuildVoice voice)
        {
            return new(voice);
        }
    }

    public class InMemoryGuildSoundEffectQueue
        : SimpleQueueChannel<ISoundEffect>, IGuildSoundEffectQueue
    {
        public IGuildVoice VoicePlayer { get; }

        public InMemoryGuildSoundEffectQueue(IGuildVoice voice)
        {
            VoicePlayer = voice;
        }
    }
}
