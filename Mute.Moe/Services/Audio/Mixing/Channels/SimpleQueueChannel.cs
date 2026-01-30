using System.Collections.Concurrent;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing.Channels;

/// <summary>
/// A mixer channel that plays audio in order
/// </summary>
/// <typeparam name="TMetadata"></typeparam>
public interface ISimpleQueueChannel<TMetadata>
    : IMixerChannel
{
    /// <summary>
    /// Skip the rest of the currently playing audio clip
    /// </summary>
    void Skip();

    /// <summary>
    /// Information about the currently playing clip (if any)
    /// </summary>
    (TMetadata Metadata, Task Completion)? Playing { get; }

    /// <summary>
    /// The queue of items waiting to play
    /// </summary>
    IEnumerable<TMetadata> Queue { get; }

    /// <summary>
    /// Add a new item to the end of the queue
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="audio"></param>
    /// <returns></returns>
    Task<Task> Enqueue(TMetadata metadata, ISampleProvider audio);
}

/// <summary>
/// Plays a queue of audio in order
/// </summary>
public class SimpleQueueChannel<T>
    : ISimpleQueueChannel<T>
{
    /// <inheritdoc />
    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private readonly ConcurrentQueue<QueueClip> _queue = new();
    private QueueClip? _playing;
    private volatile bool _skip;

    /// <inheritdoc />
    public bool IsPlaying => !_queue.IsEmpty || _playing.HasValue;

    /// <inheritdoc/>
    public (T Metadata, Task Completion)? Playing => _playing.HasValue ? (_playing.Value.Metadata, _playing.Value.Completion) : default;

    /// <inheritdoc />
    public IEnumerable<T> Queue => _queue.Select(a => a.Metadata).ToArray();

    /// <summary>
    /// Add a new item to the end of the queue. Audio will be automatically disposed once played.
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="audio"></param>
    /// <typeparam name="TAudio"></typeparam>
    /// <returns></returns>
    public Task<Task> Enqueue<TAudio>(T metadata, TAudio audio)
        where TAudio : ISampleProvider, IDisposable
    {
        var q = new QueueClip(metadata, new WdlResamplingSampleProvider(audio, WaveFormat.SampleRate).ToMono());
        q.Completion.ContinueWith(_ => audio.Dispose());

        _queue.Enqueue(q);

        return Task.FromResult(q.Completion);
    }

    /// <inheritdoc />
    public Task<Task> Enqueue(T metadata, ISampleProvider audio)
    {
        var q = new QueueClip(metadata, new WdlResamplingSampleProvider(audio, WaveFormat.SampleRate).ToMono());

        _queue.Enqueue(q);

        return Task.FromResult(q.Completion);
    }

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        //Clear the buffer before reading into it
        Array.Clear(buffer, offset, count);

        //Skip track if requested
        if (_skip)
        {
            _playing?.Dispose();
            _playing = default;

            _skip = false;
        }

        //Try to fetch the next queue item
        if (!_playing.HasValue && _queue.TryDequeue(out var p))
            _playing = p;

        //If we're not playing anything, just return a buffer of zeroes
        if (!_playing.HasValue)
            return count;
            
        //Read audio from source
        var read = _playing.Value.Samples.Read(buffer, offset, count);

        //If the entire buffer was not filled this this item is complete, remove it from _playing. Next time we'll start the next item in the queue
        if (read < count)
        {
            _playing.Value.Dispose();
            _playing = default;
        }

        return count;
    }

    /// <inheritdoc />
    public void Skip()
    {
        _skip = true;
    }

    /// <inheritdoc />
    public void Stop()
    {
        //Clear queue
        while (_queue.TryDequeue(out var dq))
            dq.Dispose();

        //Skip currently playing track
        Skip();
    }

    private readonly record struct QueueClip(T Metadata, ISampleProvider Samples)
        : IDisposable
    {
        private readonly TaskCompletionSource<bool> _onCompletion = new();

        public Task Completion => _onCompletion.Task;

        public void Dispose()
        {
            _onCompletion.SetResult(true);
        }
    }
}