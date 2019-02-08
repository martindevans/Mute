using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Mixing
{
    /// <summary>
    /// Applies a gain to a signal
    /// </summary>
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
            //Copy upstream into the given buffer
            var read = _upstream.Read(buffer, offset, count);

            //Apply gain
            for (var i = 0; i < read; i++)
                buffer[offset + i] *= _gain;

            //Return number of samples read
            return read;
        }
    }
}
