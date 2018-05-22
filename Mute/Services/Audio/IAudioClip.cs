using System.IO;
using NAudio.Wave;

namespace Mute.Services.Audio
{
    public interface IAudioClip
    {
        /// <summary>
        /// Get the name of this clip
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get the type of clip this is
        /// </summary>
        AudioClipType Type { get; }

        /// <summary>
        /// Open a stream to begin reading this clip
        /// </summary>
        /// <returns></returns>
        ISampleProvider Open();
    }

    public enum AudioClipType
    {
        Music,
        Voice,
        Other
    }
}
