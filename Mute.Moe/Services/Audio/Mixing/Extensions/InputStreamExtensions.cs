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

        public int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(1024, count);

            var r = _input.Read(buffer, offset, count);
            return r;
        }
    }
}