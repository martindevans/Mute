using NAudio.Wave;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;

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
#if !NCRUNCH
            OpusPcmSoftClip(samples.AsSpan(), samples.Count / _memory.Length, _memory.Length, _memory);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SampleIndex(int frame, int channel, int channels) => frame * channels + channel;

        private static void OpusPcmSoftClip(Span<float> pcm, int frameSize, int channels, Span<float> declipMem)
        {
            if (channels < 1 || frameSize < 1)
                return;

            // Saturate to ±2
            TensorPrimitives.Clamp(pcm, -2, 2, pcm);

            for (var channel = 0; channel < channels; channel++)
            {
                var a = declipMem[channel];

                // Continue previous frame's correction
                for (var i = 0; i < frameSize; i++)
                {
                    var idx = SampleIndex(i, channel, channels);

                    if (pcm[idx] * a >= 0)
                        break;

                    pcm[idx] += a * pcm[idx] * pcm[idx];
                }

                var curr = 0;
                var x0 = pcm[channel];

                while (true)
                {
                    int start;
                    int end;

                    // Find first clipped sample
                    var i = curr;
                    for (; i < frameSize; i++)
                    {
                        var x = pcm[SampleIndex(i, channel, channels)];

                        if (x is > 1.0f or < -1.0f)
                            break;
                    }
                    
                    if (i == frameSize)
                    {
                        a = 0;
                        break;
                    }

                    var peakPos = start = end = i;
                    var maxVal = MathF.Abs(pcm[SampleIndex(i, channel, channels)]);

                    // Find previous zero crossing
                    while (start > 0 && pcm[SampleIndex(i, channel, channels)] * pcm[SampleIndex(start - 1, channel, channels)] >= 0)
                    {
                        start--;
                    }

                    // Find next zero crossing and largest peak
                    while (end < frameSize && pcm[SampleIndex(i, channel, channels)] * pcm[SampleIndex(end, channel, channels)] >= 0)
                    {
                        var abs = MathF.Abs(pcm[SampleIndex(end, channel, channels)]);

                        if (abs > maxVal)
                        {
                            maxVal = abs;
                            peakPos = end;
                        }

                        end++;
                    }

                    var special = start == 0 && pcm[SampleIndex(i, channel, channels)] * pcm[channel] >= 0;

                    // Compute correction coefficient
                    a = (maxVal - 1.0f) / (maxVal * maxVal);

                    if (pcm[SampleIndex(i, channel, channels)] > 0)
                        a = -a;

                    // Apply soft clipping
                    for (i = start; i < end; i++)
                    {
                        var idx = SampleIndex(i, channel, channels);
                        pcm[idx] += a * pcm[idx] * pcm[idx];
                    }

                    // Beginning-of-frame discontinuity fix
                    if (special && peakPos >= 2)
                    {
                        var offset = x0 - pcm[channel];
                        var delta = offset / peakPos;

                        for (i = curr; i < peakPos; i++)
                        {
                            offset -= delta;

                            var idx = SampleIndex(i, channel, channels);

                            pcm[idx] += offset;
                            pcm[idx] = Math.Clamp(pcm[idx], -1.0f, 1.0f);
                        }
                    }

                    curr = end;

                    if (curr == frameSize)
                        break;
                }

                declipMem[channel] = a;
            }
        }
    }
}