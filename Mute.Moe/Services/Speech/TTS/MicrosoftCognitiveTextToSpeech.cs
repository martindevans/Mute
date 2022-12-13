using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Mute.Moe.Services.Audio.Clips;
using Mute.Moe.Services.Music;
using NAudio.Wave;

namespace Mute.Moe.Services.Speech.TTS;

public class MicrosoftCognitiveTextToSpeech
    : ITextToSpeech
{
    private readonly SpeechConfig _config;

    public MicrosoftCognitiveTextToSpeech(Configuration config)
    {
        _config = SpeechConfig.FromSubscription(
            config.TTS?.MsCognitive?.Key ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Key)),
            config.TTS?.MsCognitive?.Region ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Region))
        );
        _config.SpeechSynthesisLanguage = config.TTS?.MsCognitive?.Language ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Language));
        _config.SpeechSynthesisVoiceName = config.TTS?.MsCognitive?.Voice ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Voice));
    }

    public async Task<IAudioClip> Synthesize(string text)
    {
        var stream = AudioOutputStream.CreatePullStream();

        //Generate voice data into stream
        using (var streamConfig = AudioConfig.FromStreamOutput(stream))
        using (var synthesizer = new SpeechSynthesizer(_config, streamConfig))
        {
            using var result = await synthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new TaskCanceledException($"{cancellation.Reason}: {cancellation.ErrorDetails}");
            }
        }

        //Create a clip which consumes this audio data
        return new AudioOutputStreamClip($"TTS:`{text}`", stream, new WaveFormat(16000, 16, 1));
    }

    public class AudioOutputStreamClip
        : IAudioClip
    {
        private readonly PullAudioOutputStream _stream;
        private readonly WaveFormat _format;

        public AudioOutputStreamClip( string name, PullAudioOutputStream stream, WaveFormat format)
        {
            Name = name;

            _stream = stream;
            _format = format;
        }

        public Task<ITrack?> Track { get; } = Task.FromResult<ITrack?>(null);

        public string Name { get; }

        public async Task<IOpenAudioClip> Open()
        {
            return new OpenAudioClipWaveWrapper<WaveProvider>(this, new WaveProvider(_stream, _format));
        }

        private class WaveProvider
            : IWaveProvider, IDisposable
        {
            private PullAudioOutputStream? _stream;

            public WaveFormat WaveFormat { get; }

            public WaveProvider(PullAudioOutputStream stream, WaveFormat format)
            {
                WaveFormat = format;
                _stream = stream;
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                if (_stream == null)
                    return 0;

                //Fast case for when the count and offset are exactly as the read method expects
                if (offset == 0 && count == buffer.Length)
                    return (int)_stream.Read(buffer);

                //We'll have to allocate an array to read into which is the right size and alignment :(
                var temp = new byte[count];
                var read = (int)_stream.Read(temp);
                Buffer.BlockCopy(temp, 0, buffer, offset, read);
                return read;
            }

            public void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
            }
        }
    }
}