using System.Collections.Concurrent;
using System.Linq;
using Mute.Moe.Services.Audio.Mixing.Extensions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing
{
    public class MultiChannelMixer
        : IWaveProvider
    {
        private static readonly WaveFormat MixingFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        private static readonly WaveFormat OutputFormat = new(48000, 16, 2);

        private readonly ConcurrentDictionary<IMixerInput, ISampleProvider> _inputMap = new();

        private readonly MixingSampleProvider _inputs;
        private readonly IWaveProvider _output;

        public WaveFormat WaveFormat => _output.WaveFormat;

        public bool IsPlaying => _inputMap.Keys.Any(a => a.IsPlaying);

        public MultiChannelMixer()
        {
            // Mix all inputs together
            _inputs = new MixingSampleProvider(MixingFormat) {ReadFully = true};

            // Apply pipeline and eventually convert to the required output format
            _output = _inputs.ToMono().AutoGainControl().Resample(OutputFormat.SampleRate).SoftClip().ToStereo().ToWaveProvider16();
        }

        int IWaveProvider.Read(byte[] buffer, int offset, int count)
        {
            return _output.Read(buffer, offset, count);
        }

        public void Add(IMixerInput input)
        {
            var samples = _inputMap.GetOrAdd(input, _ => input.ToMono().Resample(MixingFormat.SampleRate));
            _inputs.AddMixerInput(samples);
        }

        public void Remove(IMixerInput input)
        {
            if (_inputMap.TryRemove(input, out var value))
                _inputs.RemoveMixerInput(value);
        }
    }

    /// <summary>
    /// Provides a continuouss stream of samples
    /// </summary>
    public interface IMixerInput
        : ISampleProvider
    {
        /// <summary>
        /// Get a value indicating if this channel is playing audio
        /// </summary>
        bool IsPlaying { get; }
    }
}
