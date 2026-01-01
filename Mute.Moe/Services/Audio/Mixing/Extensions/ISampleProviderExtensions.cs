
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing.Extensions;

/// <summary>
/// Extensions for <see cref="ISampleProvider"/>
/// </summary>
public static class ISampleProviderExtensions
{
    /// <summary>
    /// Resample to a new sample rate
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="sampleRate"></param>
    /// <returns></returns>
    public static ISampleProvider Resample(this ISampleProvider provider, int sampleRate)
    {
        return provider.WaveFormat.SampleRate == sampleRate
             ? provider
             : new WdlResamplingSampleProvider(provider, sampleRate);
    }

    /// <summary>
    /// Apply soft clipping
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static ISampleProvider SoftClip(this ISampleProvider provider)
    {
        return new SoftClipSampleProvider(provider);
    }

    /// <summary>
    /// Apply automatic gain control
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="maxVolume"></param>
    /// <param name="minVolume"></param>
    /// <param name="minGain"></param>
    /// <param name="maxGain"></param>
    /// <param name="upRate"></param>
    /// <param name="downRate"></param>
    /// <returns></returns>
    public static ISampleProvider AutoGainControl(
        this ISampleProvider provider,
        float maxVolume = -10,
        float minVolume = -25,
        float minGain = 0.5f,
        float maxGain = 3f,
        float upRate = 0.5f,
        float downRate = -2)
    {
        return new AutoGainControl(provider, maxVolume, minVolume, minGain, maxGain, upRate, downRate);
    }
}