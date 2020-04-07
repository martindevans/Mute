using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Speech;
using Mute.Moe.Services.Speech.TTS;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public class EnhanceYourChill
        : IUnsuccessfulCommandPostprocessor
    {
        private readonly Configuration _config;
        private readonly Random _random;
        private readonly ITextToSpeech _tts;
        private readonly IGuildSpeechQueueCollection _ttsQueue;

        public uint Order => 0;

        private readonly IReadOnlyList<string[]> _responses = new[] {
            new[] { "What?" },
            new[] { "Enhance your calm" },
            new[] { "Alright, don't shout at me!" },
            new[] { "EVERYONE STAY CALM", "NOBODY PANIC!" }
        };

        public EnhanceYourChill(Configuration config, Random random, ITextToSpeech tts, IGuildSpeechQueueCollection ttsQueue)
        {
            _config = config;
            _random = random;
            _tts = tts;
            _ttsQueue = ttsQueue;
        }

        public async Task<bool> Process(MuteCommandContext context, IResult result)
        {
            // Only respond to strings of all <prefix char>
            if (context.Message.Content.Any(a => a != _config.PrefixCharacter))
                return false;

            // Don't respond all the time
            if (_random.NextDouble() < 0.5f)
                return true;

            // Choose a random response for TTS ot text
            var responses = _responses.Random(_random);

            // If user is in voice and bot is already in channel, use TTS to respond
            if (context.User is IVoiceState vs && vs.VoiceChannel != null && context.Guild != null)
            {
                var ttsQueue = await _ttsQueue.Get(context.Guild.Id);
                if (ttsQueue.VoicePlayer.Channel?.Id == vs.VoiceChannel.Id)
                {
                    await responses.ToAsyncEnumerable().Select(_tts.Synthesize).ForEachAsync(async clip => {
                        var audio = await clip;
                        await ttsQueue.Enqueue(audio.Name, await audio.Open());
                    });

                    return true;
                }
            }

            // Respond with some kind of repeated character
            if (_random.NextDouble() < 0.5f)
            {
                var character = "!!!!???¡".Random(_random);

                await context.Channel.SendMessageAsync(new string(Enumerable.Repeat(character, context.Message.Content.Length + 1).ToArray()));
                return true;
            }

            // Respond with one of a selection of messages
            foreach (var response in responses)
            {
                await context.Channel.TypingReplyAsync(response);
                await Task.Delay(200);
            }
            return true;
        }
    }
}
