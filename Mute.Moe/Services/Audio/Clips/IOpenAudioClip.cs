using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Clips;

public interface IOpenAudioClip
    : ISampleProvider, IDisposable
{
    IAudioClip Clip { get; }
}

public class OpenAudioClipSamplesWrapper<T>
    : IOpenAudioClip
    where T : ISampleProvider, IDisposable
{
    public IAudioClip Clip { get; }

    private readonly T _upstream;

    public OpenAudioClipSamplesWrapper(IAudioClip clip, T upstream)
    {
        Clip = clip;
        _upstream = upstream;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return _upstream.Read(buffer, offset, count);
    }

    public WaveFormat WaveFormat => _upstream.WaveFormat;

    public void Dispose()
    {
        _upstream.Dispose();
    }
}

public class OpenAudioClipWaveWrapper<T>
    : IOpenAudioClip
    where T : IWaveProvider, IDisposable
{
    public IAudioClip Clip { get; }

    private readonly T _upstream;
    private readonly ISampleProvider _samples;

    public OpenAudioClipWaveWrapper(IAudioClip clip, T upstream)
    {
        Clip = clip;

        _upstream = upstream;
        _samples = _upstream.ToSampleProvider();
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return _samples.Read(buffer, offset, count);
    }

    public WaveFormat WaveFormat => _samples.WaveFormat;

    public void Dispose()
    {
        _upstream.Dispose();
    }
}