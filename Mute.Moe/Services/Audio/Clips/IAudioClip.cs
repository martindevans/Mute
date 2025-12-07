using System.Threading.Tasks;

namespace Mute.Moe.Services.Audio.Clips;

/// <summary>
/// An audio clip which can be opened to provide a stream of audio samples
/// </summary>
public interface IAudioClip
{
    /// <summary>
    /// Get the name of this clip
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Open a stream to begin reading this clip
    /// </summary>
    /// <returns></returns>
    Task<IOpenAudioClip> Open();
}