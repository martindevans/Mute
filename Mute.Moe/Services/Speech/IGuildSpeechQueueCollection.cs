using System.Threading.Tasks;
using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.Speech;

public interface IGuildSpeechQueueCollection
{
    Task<IGuildSpeechQueue> Get(ulong guild);
}

public interface IGuildSpeechQueue
    : ISimpleQueueChannel<string>
{
    IGuildVoice VoicePlayer { get; }
}

public class InMemoryGuildSpeechQueueCollection(IGuildVoiceCollection voice)
    : BaseInMemoryAudioPlayerQueueCollection<InMemoryGuildSpeechQueue, IGuildSpeechQueue>(voice), IGuildSpeechQueueCollection
{
    protected override InMemoryGuildSpeechQueue Create(IGuildVoice v)
    {
        return new(v);
    }
}

public class InMemoryGuildSpeechQueue(IGuildVoice voice)
    : SimpleQueueChannel<string>, IGuildSpeechQueue
{
    public IGuildVoice VoicePlayer { get; } = voice;
}