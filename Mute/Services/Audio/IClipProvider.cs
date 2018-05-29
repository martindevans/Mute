using JetBrains.Annotations;
using Mute.Services.Audio.Clips;

namespace Mute.Services.Audio
{
    public interface IClipProvider
    {
        [CanBeNull] IAudioClip GetNextClip();
    }
}
