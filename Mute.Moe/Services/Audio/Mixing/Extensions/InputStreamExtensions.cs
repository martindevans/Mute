using Discord.Audio.Streams;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Mixing.Extensions;

public static class InputStreamExtensions
{
    public static IWaveProvider AsWaveProvider(this InputStream input, WaveFormat format)
    {
        return new InputStreamWrapper(input, format);
    }

    private class InputStreamWrapper
        : IWaveProvider
    {
        private readonly InputStream _input;
        public WaveFormat WaveFormat { get; }

        public InputStreamWrapper(InputStream input, WaveFormat format)
        {
            WaveFormat = format;
            _input = input;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(1024, count);

            var r = _input.Read(buffer, offset, count);
            return r;
        }
    }
}