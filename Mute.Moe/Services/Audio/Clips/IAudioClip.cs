using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Services.Music;

namespace Mute.Moe.Services.Audio.Clips
{
    public interface IAudioClip
    {
        [NotNull, ItemCanBeNull] Task<ITrack> Track { get; }

        /// <summary>
        /// Get the name of this clip
        /// </summary>
        [NotNull] string Name { get; }

        /// <summary>
        /// Open a stream to begin reading this clip
        /// </summary>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IOpenAudioClip> Open();
    }
}
