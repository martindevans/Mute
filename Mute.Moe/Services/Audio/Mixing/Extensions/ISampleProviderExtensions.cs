
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing.Extensions;

// ReSharper disable once InconsistentNaming
public static class ISampleProviderExtensions
{
    public static ISampleProvider Resample(this ISampleProvider provider, int sampleRate)
    {
        if (provider.WaveFormat.SampleRate == sampleRate)
            return provider;
        return new WdlResamplingSampleProvider(provider, sampleRate);
    }

    public static ISampleProvider SoftClip(this ISampleProvider provider)
    {
        return new SoftClipSampleProvider(provider);
    }

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