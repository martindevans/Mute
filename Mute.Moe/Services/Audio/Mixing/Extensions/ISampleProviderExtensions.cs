using JetBrains.Annotations;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class ISampleProviderExtensions
    {
        [NotNull] public static ISampleProvider Resample([NotNull] this ISampleProvider provider, int sampleRate)
        {
            if (provider.WaveFormat.SampleRate == sampleRate)
                return provider;
            else
                return new WdlResamplingSampleProvider(provider, sampleRate);
        }

        [NotNull] public static ISampleProvider SoftClip([NotNull] this ISampleProvider provider)
        {
            return new SoftClipSampleProvider(provider);
        }

        [NotNull] public static ISampleProvider AutoGainControl(
            [NotNull] this ISampleProvider provider,
            float maxVolume = -10, float minVolume = -25,
            float minGain = 0.5f, float maxGain = 3f,
            float upRate = 0.5f, float downRate = -2)
        {
            return new AutoGainControl(provider, maxVolume, minVolume, minGain, maxGain, upRate, downRate);
        }
    }
}
