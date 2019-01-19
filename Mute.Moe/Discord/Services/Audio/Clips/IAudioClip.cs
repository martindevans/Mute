using NAudio.Wave;

namespace Mute.Moe.Discord.Services.Audio.Clips
{
    public interface IAudioClip
    {
        /// <summary>
        /// Get the name of this clip
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if this file is ready to be opened
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Open a stream to begin reading this clip
        /// </summary>
        /// <returns></returns>
        ISampleProvider Open();
    }
}
