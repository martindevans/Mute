using JetBrains.Annotations;

namespace Mute.Services.Audio
{
    public interface IClipProvider
    {
        [CanBeNull] IAudioClip GetNextClip();
    }
}
