using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing.Channels
{
    public interface ISimpleQueueChannel<TMetadata>
        : IMixerChannel
    {
        void Skip();

        (TMetadata Metadata, Task Completion) Playing { get; }

        IEnumerable<TMetadata> Queue { get; }

        Task<Task> Enqueue(TMetadata metadata, ISampleProvider audio);
    }

    /// <summary>
    /// Plays a queue of audio in order
    /// </summary>
    public class SimpleQueueChannel<T>
        : ISimpleQueueChannel<T>
    {
        public WaveFormat WaveFormat { get; }

        private readonly ConcurrentQueue<QueueClip> _queue = new ConcurrentQueue<QueueClip>();
        private QueueClip _playing;
        private volatile bool _skip;

        public bool IsPlaying => _queue.Count > 0 || _playing.Samples != null;

        public (T Metadata, Task Completion) Playing => (_playing.Metadata, _playing.Completion);

        [NotNull] public IEnumerable<T> Queue => _queue.Select(a => a.Metadata).ToArray();

        public SimpleQueueChannel()
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        }

        [NotNull] public Task<Task> Enqueue<TAudio>(T metadata, TAudio audio)
            where TAudio : ISampleProvider, IDisposable
        {
            var q = new QueueClip(metadata, new WdlResamplingSampleProvider(audio, WaveFormat.SampleRate).ToMono());
            q.Completion.ContinueWith(_ => audio.Dispose());

            _queue.Enqueue(q);

            return Task.FromResult(q.Completion);
        }

        [NotNull] public Task<Task> Enqueue(T metadata, ISampleProvider audio)
        {
            var q = new QueueClip(metadata, new WdlResamplingSampleProvider(audio, WaveFormat.SampleRate).ToMono());

            _queue.Enqueue(q);

            return Task.FromResult(q.Completion);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            //Clear the buffer before reading into it
            Array.Clear(buffer, offset, count);

            //Skip track if requested
            if (_skip)
            {
                if (_playing.Samples != null)
                    _playing.Dispose();
                _playing = default;

                _skip = false;
            }

            //Try to fetch the next queue item
            if (_playing.Samples == null)
                _queue.TryDequeue(out _playing);

            //If we're not playing anything, just return a buffer of zeroes
            if (_playing.Samples == null)
                return count;
            
            //Read audio from source
            var read = _playing.Samples.Read(buffer, offset, count);

            //If the entire buffer was not filled this this item is complete, remove it from _playing. Next time we'll start the next item in the queue
            if (read < count)
            {
                _playing.Dispose();
                _playing = default;
            }

            return count;
        }

        public void Skip()
        {
            _skip = true;
        }

        public void Stop()
        {
            //Clear queue
            while (_queue.TryDequeue(out var dq))
                dq.Dispose();

            //Skip currently playing track
            Skip();
        }

        private struct QueueClip
            : IDisposable
        {
            public readonly T Metadata;
            public readonly ISampleProvider Samples;

            private readonly TaskCompletionSource<bool> _onCompletion;

            public QueueClip(T metadata, ISampleProvider samples)
            {
                Metadata = metadata;
                Samples = samples;
                _onCompletion = new TaskCompletionSource<bool>();
            }

            public Task Completion => _onCompletion.Task;

            public void Dispose()
            {
                _onCompletion.SetResult(true);
            }
        }
    }
}
