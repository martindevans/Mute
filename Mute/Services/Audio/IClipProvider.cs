using System.Threading.Tasks;
using Mute.Services.Audio.Clips;

namespace Mute.Services.Audio
{
    public interface IClipProvider
    {
        QueuedClip? GetNextClip();
    }

    public struct QueuedClip
    {
        public readonly IAudioClip Clip;
        public readonly TaskCompletionSource<bool> TaskCompletion;

        public QueuedClip(IAudioClip clip, TaskCompletionSource<bool> taskCompletion)
        {
            Clip = clip;
            TaskCompletion = taskCompletion;
        }
    }
}
