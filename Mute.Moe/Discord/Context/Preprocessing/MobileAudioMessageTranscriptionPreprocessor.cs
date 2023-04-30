using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Concentus.Oggfile;
using Concentus.Structs;
using Discord;
using Mute.Moe.Services.Speech.STT;
using Mute.Moe.Utilities;
using NAudio.Wave;

namespace Mute.Moe.Discord.Context.Preprocessing
{
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

            await context.Message.AddReactionAsync(new Emoji(EmojiLookup.StudioMicrophone));
            try
            {
                // Recognition
                var words = _stt.OneShotRecognition(audio);
                var result = string.Join(" ", words.Select(a => a.Text));

                // Respond with result
                var embed = new EmbedBuilder()
                           .WithTitle("Transcription")
                           .WithDescription(result)
                           .WithAuthor(context.User)
                           .WithCurrentTimestamp();
                await context.Channel.SendMessageAsync(embed: embed.Build(), allowedMentions: AllowedMentions.None, messageReference: context.Message.Reference);
            }
            finally
            {
                await context.Message.RemoveReactionAsync(new Emoji(EmojiLookup.StudioMicrophone), context.Client.CurrentUser);
            }
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

                foreach (var sample in packet)
                    samples.Add(sample * (1f / 32768f));
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
}
