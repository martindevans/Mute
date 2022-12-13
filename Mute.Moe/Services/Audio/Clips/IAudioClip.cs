using System.Threading.Tasks;

using Mute.Moe.Services.Music;

namespace Mute.Moe.Services.Audio.Clips;

public interface IAudioClip
{
    Task<ITrack?> Track { get; }

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