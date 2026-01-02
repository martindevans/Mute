using System.Runtime.InteropServices;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Mixing;

/// <summary>
/// Apply a soft clipping to the signal (so clipping does not generate unpleasant clicks)
/// </summary>
public partial class SoftClipSampleProvider(ISampleProvider upstream)
    : ISampleProvider
{
    private readonly OpusSoftClip _clipper = new(upstream.WaveFormat.Channels);

    /// <inheritdoc />
    public WaveFormat WaveFormat => upstream.WaveFormat;

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        var read = upstream.Read(buffer, offset, count);
        _clipper.Clip(new ArraySegment<float>(buffer, offset, read));
        return read;
    }

    /// <summary>
    /// Applies soft clipping to a signal
    /// </summary>
    public sealed partial class OpusSoftClip
    {
        private readonly float[] _memory;

        /// <summary>
        /// Create a new soft clip state
        /// </summary>
        /// <param name="channels"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public OpusSoftClip(int channels = 1)
        {
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be > 0");

            _memory = new float[channels];
        }

        /// <summary>
        /// Apply soft clipping to a set of samples
        /// </summary>
        /// <param name="samples"></param>
        public void Clip(ArraySegment<float> samples)
        {
            unsafe
            {
#if !NCRUNCH
                using var pin = samples.AsMemory().Pin();

                opus_pcm_soft_clip(
                    (IntPtr)pin.Pointer,
                    samples.Count / _memory.Length,
                    _memory.Length,
                    _memory
                );
#endif
            }
        }

        [LibraryImport("opus")]
        [UnmanagedCallConv(CallConvs = [ typeof(System.Runtime.CompilerServices.CallConvCdecl) ])]
        private static partial void opus_pcm_soft_clip(nint pcm, int frameSize, int channels, [In, Out] float[] softClipMem);
    }
}