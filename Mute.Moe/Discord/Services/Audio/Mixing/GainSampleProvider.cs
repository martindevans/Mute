using NAudio.Wave;

namespace Mute.Moe.Discord.Services.Audio.Mixing
{
    public class GainSampleProvider
        : ISampleProvider
    {
        private readonly ISampleProvider _upstream;
        private readonly float _gain;

        public WaveFormat WaveFormat => _upstream.WaveFormat;

        public GainSampleProvider(ISampleProvider upstream, float gain)
        {
            _upstream = upstream;
            _gain = gain;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _upstream.Read(buffer, offset, count);
            for (var i = 0; i < read; i++)
                buffer[offset + i] *= _gain;
            return read;
        }
    }
}
