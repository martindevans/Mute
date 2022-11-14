using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public class EnhanceYourChill
        : IUnsuccessfulCommandPostprocessor
    {
        private readonly Configuration _config;
        private readonly Random _random;

        public uint Order => 0;

        private readonly IReadOnlyList<string[]> _responses = new[] {
            new[] { "Enhance your calm" },
            new[] { "Alright, don't shout at me!" },
            new[] { "EVERYONE STAY CALM", "NOBODY PANIC!", "IT'S NOT AS BAD AS IT LOOKS" }
        };

        public EnhanceYourChill(Configuration config, Random random)
        {
            _config = config;
            _random = random;
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
