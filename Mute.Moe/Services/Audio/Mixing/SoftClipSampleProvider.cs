using System;
using System.Runtime.InteropServices;

using Mute.Moe.Extensions;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Mixing
{
    /// <summary>
    /// Apply a soft clipping to the signal (so clipping does not generate unpleasant clicks)
    /// </summary>
    public class SoftClipSampleProvider
        : ISampleProvider
    {
        private readonly ISampleProvider _upstream;
        private readonly OpusSoftClip _clipper;

        public WaveFormat WaveFormat => _upstream.WaveFormat;

        public SoftClipSampleProvider( ISampleProvider upstream)
        {
            _upstream = upstream;
            _clipper = new OpusSoftClip(upstream.WaveFormat.Channels);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _upstream.Read(buffer, offset, count);
            _clipper.Clip(new ArraySegment<float>(buffer, offset, read));
            return read;
        }

        /// <summary>
        /// Applies soft clipping to a signal
        /// </summary>
        public sealed class OpusSoftClip
        {
            private readonly float[] _memory;

            public OpusSoftClip(int channels = 1)
            {
                if (channels <= 0)
                    throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be > 0");

                _memory = new float[channels];
            }

            public void Clip(ArraySegment<float> samples)
            {
#if !NCRUNCH
                using var handle = samples.Pin();
                opus_pcm_soft_clip(
                    handle.Ptr,
                    samples.Count / _memory.Length,
                    _memory.Length,
                    _memory
                );
#endif
            }

            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // Naming Styles
            private static extern void opus_pcm_soft_clip(IntPtr pcm, int frameSize, int channels, float[] softClipMem);
#pragma warning restore IDE1006 // Naming Styles
        }
    }
}
