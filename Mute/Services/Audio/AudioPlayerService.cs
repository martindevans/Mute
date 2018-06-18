using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Services.Audio.Clips;

namespace Mute.Services.Audio
{
    public class AudioPlayerService
        : IClipProvider
    {
        private ThreadedAudioPlayer _player;

        private readonly object _queueLock = new object();
        private readonly List<QueuedClip> _queue = new List<QueuedClip>();
        [NotNull] public IReadOnlyList<IAudioClip> Queue
        {
            get
            {
                lock (_queue)
                    return _queue.Select(a => a.Clip).ToArray();
            }
        }

        private IVoiceChannel _channel;
        [CanBeNull] public IVoiceChannel Channel
        {
            get => _channel;
            set
            {
                if (_channel == value)
                    return;
                _channel = value;

                if (_player != null)
                {
                    // Suspend current playback and immediately resume it.
                    // Resuming will pick up the new channel value
                    Pause();
                    Resume();
                }
            }
        }

        [CanBeNull] public IAudioClip Playing => _player?.Playing;

        /// <summary>
        /// Skip the currently playing track
        /// </summary>
        public void Skip() => _player.Skip();

        /// <summary>
        /// Pause playback
        /// </summary>
        public void Pause() => throw new NotImplementedException();

        /// <summary>
        /// Resume paused playback
        /// </summary>
        public void Resume() => throw new NotImplementedException();

        /// <summary>
        /// Stop playback and clear the queue
        /// </summary>
        public void Stop()
        {
            var p = _player;
            if (p != null)
                p.Stop();

            lock (_queueLock)
                _queue.Clear();    
            
        }

        /// <summary>
        /// Add an audio clip to the end of the current playback queue
        /// </summary>
        /// <returns>A task which will complete when the song has been played or skipped</returns>
        public Task<bool> Enqueue(IAudioClip clip)
        {
            var qc = new QueuedClip(clip, new TaskCompletionSource<bool>());

            lock (_queueLock)
                _queue.Add(qc);

            return qc.TaskCompletion.Task;
        }

        /// <summary>
        /// Start playing the current queue
        /// </summary>
        public void Play()
        {
            if (_channel == null)
                throw new InvalidOperationException("Cannot start playing when channel is null");

            if (_player == null || !_player.IsAlive)
            {
                _player = new ThreadedAudioPlayer(_channel, this);
                _player.Start();
            }
        }

        QueuedClip? IClipProvider.GetNextClip()
        {
            lock (_queueLock)
            {
                if (_queue.Count == 0)
                    return null;

                var next = _queue[0];
                _queue.RemoveAt(0);
                return next;
            }
        }

        public void Shuffle()
        {
            lock (_queueLock)
            {
                //Get all clips in queue and order them randomly
                var r = new Random();
                var clips = _queue.Select(a => (a, r.Next())).OrderBy(a => a.Item2).Select(a => a.Item1).ToArray();

                //Reinsert al those clips in the new order
                _queue.Clear();
                _queue.AddRange(clips);
            }
        }
    }
}
