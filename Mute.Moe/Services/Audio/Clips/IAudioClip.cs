using System.Threading.Tasks;

namespace Mute.Moe.Services.Audio.Clips;

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