using System.Threading.Tasks;
using Mute.Moe.Services.Audio.Mixing;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Audio;

/// <summary>
/// Stores a set of <see cref="IMixerChannel"/>s, keyed by a <see cref="ulong"/>
/// </summary>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TInterface"></typeparam>
public abstract class BaseInMemoryAudioPlayerQueueCollection<TValue, TInterface>
    where TValue : IMixerChannel, TInterface
{
    private readonly IGuildVoiceCollection _voice;

    private readonly AsyncLock _lookupLock = new();
    private readonly Dictionary<ulong, TValue> _lookup = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="voice"></param>
    protected BaseInMemoryAudioPlayerQueueCollection(IGuildVoiceCollection voice)
    {
        _voice = voice;
    }

    /// <summary>
    /// Get the mixer for the gicen guild
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Create an object for the given guild voice
    /// </summary>
    /// <param name="voice"></param>
    /// <returns></returns>
    protected abstract TValue Create(IGuildVoice voice);
}