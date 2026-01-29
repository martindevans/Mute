using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Clips;

/// <summary>
/// An audio clip that has been opened for playback
/// </summary>
public interface IOpenAudioClip
    : ISampleProvider, IDisposable
{
    /// <summary>
    /// The audio clip which has been opened
    /// </summary>
    IAudioClip Clip { get; }
}

/// <summary>
/// Wraps a <see cref="ISampleProvider"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class OpenAudioClipSamplesWrapper<T>
    : IOpenAudioClip
    where T : class, ISampleProvider, IDisposable
{
    /// <inheritdoc />
    public IAudioClip Clip { get; }

    private readonly T _upstream;

    /// <summary>
    /// Create a new wrapper
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="upstream"></param>
    public OpenAudioClipSamplesWrapper(IAudioClip clip, T upstream)
    {
        Clip = clip;
        _upstream = upstream;
    }

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        return _upstream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public WaveFormat WaveFormat => _upstream.WaveFormat;

    /// <inheritdoc />
    public void Dispose()
    {
        _upstream.Dispose();
    }
}

/// <summary>
/// Wraps an <see cref="IWaveProvider"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class OpenAudioClipWaveWrapper<T>
    : IOpenAudioClip
    where T : class, IWaveProvider, IDisposable
{
    /// <inheritdoc />
    public IAudioClip Clip { get; }

    private readonly T _upstream;
    private readonly ISampleProvider _samples;

    /// <summary>
    /// Create a new wrapper
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="upstream"></param>
    public OpenAudioClipWaveWrapper(IAudioClip clip, T upstream)
    {
        Clip = clip;

        _upstream = upstream;
        _samples = _upstream.ToSampleProvider();
    }

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        return _samples.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public WaveFormat WaveFormat => _samples.WaveFormat;

    /// <inheritdoc />
    public void Dispose()
    {
        _upstream.Dispose();
    }
}