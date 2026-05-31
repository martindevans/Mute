using NAudio.Wave;
using System.Numerics.Tensors;

namespace Mute.Moe.Services.Audio.Mixing;

/// <summary>
/// Apply a soft clipping to the signal (so clipping does not generate unpleasant clicks)
/// </summary>
public partial class SoftClipSampleProvider(ISampleProvider upstream)
    : ISampleProvider
{
    /// <inheritdoc />
    public WaveFormat WaveFormat => upstream.WaveFormat;

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        var read = upstream.Read(buffer, offset, count);

        var span = buffer.AsSpan(offset, read);
        
        // y = tanh(1.15 * x)
        TensorPrimitives.Multiply(span, 1.15f, span);
        TensorPrimitives.Tanh(span, span);

        return read;
    }
}