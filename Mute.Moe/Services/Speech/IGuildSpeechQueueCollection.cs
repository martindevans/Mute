using System.Threading.Tasks;
using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.Speech;

/// <summary>
/// Stores speech queues, one per guild
/// </summary>
public interface IGuildSpeechQueueCollection
{
    /// <summary>
    /// Get the speech queue for the guild with the given ID
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    Task<IGuildSpeechQueue> Get(ulong guild);
}

/// <summary>
/// The speech queue for a guild
/// </summary>
public interface IGuildSpeechQueue
    : ISimpleQueueChannel<string>
{
    /// <summary>
    /// The voice player for this queue
    /// </summary>
    IGuildVoice VoicePlayer { get; }
}

/// <inheritdoc cref="IGuildSpeechQueueCollection" />
public class InMemoryGuildSpeechQueueCollection(IGuildVoiceCollection voice)
    : BaseInMemoryAudioPlayerQueueCollection<InMemoryGuildSpeechQueue, IGuildSpeechQueue>(voice), IGuildSpeechQueueCollection
{
    /// <inheritdoc />
    protected override InMemoryGuildSpeechQueue Create(IGuildVoice v)
    {
        return new(v);
    }
}

/// <summary>
/// Allowed audio clips to be queued up to be played back in the guild voice chat
/// </summary>
/// <param name="voice"></param>
public class InMemoryGuildSpeechQueue(IGuildVoice voice)
    : SimpleQueueChannel<string>, IGuildSpeechQueue
{
    /// <inheritdoc />
    public IGuildVoice VoicePlayer { get; } = voice;
}