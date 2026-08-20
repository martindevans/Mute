using System.Buffers;
using Discord.Audio.Streams;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Mixing.Extensions;

/// <summary>
/// Extensions for <see cref="InputStream"/>
/// </summary>
public static class InputStreamExtensions
{
    /// <summary>
    /// Convert <see cref="InputStream"/> to an NAudio <see cref="IWaveProvider"/>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static IWaveProvider AsWaveProvider(this InputStream input, WaveFormat format)
    {
        return new InputStreamWrapper(input, format);
    }

    private class InputStreamWrapper(InputStream _input, WaveFormat _format)
        : IWaveProvider
    {
        public WaveFormat WaveFormat => _format;

        public int Read(Span<byte> buffer)
        {
            var count = Math.Min(1024, buffer.Length);

            var rented = ArrayPool<byte>.Shared.Rent(count);
            try
            {
                var r = _input.Read(rented, 0, count);
                rented.AsSpan(0, r).CopyTo(buffer);
                return r;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}