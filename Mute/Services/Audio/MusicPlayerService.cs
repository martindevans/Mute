using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Services.Audio.Clips;
using Mute.Services.Audio.Playback;

namespace Mute.Services.Audio
{
    public class MusicPlayerService
    {
        private readonly SimpleQueueChannel<IAudioClip> _queue;

        [NotNull] public IEnumerable<IAudioClip> Queue => _queue.Queue;

        public (IAudioClip, Task) Playing => _queue.Playing;

        public MusicPlayerService([NotNull] MultichannelAudioService audio)
        {
            _queue = new SimpleQueueChannel<IAudioClip>();

            audio.Open(_queue);
        }

        /// <summary>
        /// Skip the currently playing track
        /// </summary>
        public void Skip() => _queue.Skip();

        /// <summary>
        /// Clear the music queue
        /// </summary>
        public void Stop()
        {
            _queue.Stop();
        }

        /// <summary>
        /// Add an audio clip to the end of the current playback queue
        /// </summary>
        /// <returns>A task which will complete when the song has been played or skipped</returns>
        public Task Enqueue([NotNull] IAudioClip clip)
        {
            return _queue.Enqueue(clip, clip.Open());
        }

        public void Shuffle()
        {
            _queue.Shuffle();
        }
    }
}
