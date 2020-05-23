using System.Collections.Generic;
using System.Threading.Tasks;

using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Audio.Mixing.Channels;
using NAudio.Wave;

namespace Mute.Moe.Services.Music
{
    public interface IGuildMusicQueueCollection
    {
        Task<IGuildMusicQueue> Get(ulong guild);
    }

    public interface IGuildMusicQueue
        : ISimpleQueueChannel<ITrack>
    {
        IGuildVoice VoicePlayer { get; }
    }

    public class InMemoryGuildMusicQueueCollection
        : BaseInMemoryAudioPlayerQueueCollection<InMemoryGuildMusicQueue, IGuildMusicQueue>, IGuildMusicQueueCollection
    {
        public InMemoryGuildMusicQueueCollection(IGuildVoiceCollection voice)
            : base(voice)
        {
        }

         protected override InMemoryGuildMusicQueue Create(IGuildVoice voice)
        {
            return new InMemoryGuildMusicQueue(voice);
        }
    }

    public class InMemoryGuildMusicQueue
        : SimpleQueueChannel<ITrack>, IGuildMusicQueue
    {
        public IGuildVoice VoicePlayer { get; }

        public InMemoryGuildMusicQueue(IGuildVoice voice)
        {
            VoicePlayer = voice;
        }
    }
}
