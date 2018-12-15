using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class Hugot
        : BaseModule, IKeyProvider
    {
        private const double ReactionChance = 0.05;
        private const double ReplyChance = 0.01;

        private readonly Random _random;

        private readonly string[] _responses = {
            "I see how it is",
            "What does he have that I don't?",
            "Curse your sudden, but inevitable, betrayal!",
            "Hmph",
        };

        private readonly Emoji[] _reactions = {
            EmojiLookup.Smirk,
            EmojiLookup.Expressionless,
            EmojiLookup.RollingEyes,
            EmojiLookup.Worried,
        };

        public Hugot(Random random)
        {
            _random = random;
        }

        [Command("hugot"), Hidden]
        public async Task DoNothing(params string[] _)
        {
            if (_random.NextDouble() < ReactionChance)
                await Context.Message.AddReactionAsync(_reactions[_random.Next(_responses.Length)]);
            else if (_random.NextDouble() <= ReplyChance)
                await TypingReplyAsync(_responses[_random.Next(_responses.Length)]);
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("hugot", 10,
                    new Decomposition("*", "I don't want to talk about him"),
                    new Decomposition("*", "I haven't seen him around for a while...")
                );
            }
        }
    }
}
