using System.Threading.Tasks;
using JetBrains.Annotations;
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

        [NotNull] protected override InMemoryGuildSoundEffectQueue Create(IGuildVoice voice)
        {
            return new InMemoryGuildSoundEffectQueue(voice);
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
