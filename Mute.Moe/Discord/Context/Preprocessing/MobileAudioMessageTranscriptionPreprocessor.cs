using System.Net.Http;
using System.Threading.Tasks;
using Concentus.Oggfile;
using Concentus.Structs;
using Discord;
using Mute.Moe.Services.Speech.STT;
using Mute.Moe.Utilities;
using NAudio.Wave;

namespace Mute.Moe.Discord.Context.Preprocessing;

public class MobileAudioMessageTranscriptionPreprocessor
    : IMessagePreprocessor
{
    private readonly ISpeechToText _stt;
    private readonly HttpClient _client;

    public MobileAudioMessageTranscriptionPreprocessor(ISpeechToText stt, IHttpClientFactory http)
    {
        _stt = stt;

        _client = http.CreateClient();
        _client.Timeout = TimeSpan.FromMilliseconds(999);
    }

    public async Task Process(MuteCommandContext context)
    {
        var audio = await GetAudio(context);
        if (audio == null)
            return;

        var transcription = (ITranscriptionReceiver)context.GetOrAdd(() => new AudioTranscription());

        await context.Message.AddReactionAsync(new Emoji(EmojiLookup.StudioMicrophone));

        // Begin recognition task
        var work = Task.Run(() =>
        {
            var words = _stt.OneShotRecognition(audio);
            var result = string.Join(" ", words.Select(a => a.Text));
            transcription.Complete(result);
            return result;
        });

        // Register a callback when this context is finished with
        context.RegisterCompletion(async _ =>
        {
            await work;

            // Display the transcription if it has not been suppressed
            if (transcription.DisplayTranscription)
            {
                // Respond with result
                var embed = new EmbedBuilder()
                           .WithTitle("Transcription")
                           .WithDescription(await transcription.GetTranscription())
                           .WithAuthor(context.User)
                           .WithCurrentTimestamp();
                await context.Channel.SendMessageAsync(embed: embed.Build(), allowedMentions: AllowedMentions.None, messageReference: context.Message.Reference);
            }

            await context.Message.RemoveReactionAsync(new Emoji(EmojiLookup.StudioMicrophone), context.Client.CurrentUser);
        });
    }

    internal interface ITranscriptionReceiver
    {
        void Complete(string result);

        bool DisplayTranscription { get; }

        Task<string> GetTranscription();
    }

    private async Task<ISampleProvider?> GetAudio(MuteCommandContext context)
    {
        // Narrow down to only voice messages
        if (context.Message.Content != "")
            return null;
        if (context.Message.Attachments.Count != 1)
            return null;
        var attachment = context.Message.Attachments.Single();
        if (attachment.ContentType != "audio/ogg")
            return null;
        if (attachment.Filename != "voice-message.ogg")
            return null;

        using var response = await _client.GetAsync(attachment.Url);
        if (!response.IsSuccessStatusCode)
            return null;

        var samples = new List<float>();
        var decoder = OpusDecoder.Create(48000, 1);
        var oggIn = new OpusOggReadStream(decoder, await response.Content.ReadAsStreamAsync());
        while (oggIn.HasNextPacket)
        {
            var packet = oggIn.DecodeNextPacket();
            if (packet == null)
                continue;

            samples.EnsureCapacity(samples.Count + packet.Length);
            for (var i = 0; i < packet.Length; i++)
            {
                var sample = packet[i];
                samples.Add(sample * (1f / 32768f));
            }
        }

        return new AudioBuffer(samples, new WaveFormat(48000, 1));
    }

    private class AudioBuffer
        : ISampleProvider
    {
        public WaveFormat WaveFormat { get; }

        private int _read;
        private readonly List<float> _samples;

        public AudioBuffer(List<float> samples, WaveFormat waveFormat)
        {
            _samples = samples;
            WaveFormat = waveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[i + offset] = _samples[_read++];

                if (_read >= _samples.Count)
                    return i;
            }

            return count;
        }
    }
}

/// <summary>
/// Context for messages which contain audio (spoken as a voice message from mobile)
/// </summary>
public class AudioTranscription
    : MobileAudioMessageTranscriptionPreprocessor.ITranscriptionReceiver
{
    private readonly TaskCompletionSource<string> _tcs = new();

    /// <summary>
    /// Whether a transcription of this messahe should be displayed, default to true.
    /// </summary>
    public bool DisplayTranscription { get; set; } = true;

    void MobileAudioMessageTranscriptionPreprocessor.ITranscriptionReceiver.Complete(string result)
    {
        _tcs.SetResult(result);
    }

    public Task<string> GetTranscription()
    {
        return _tcs.Task;
    }
}