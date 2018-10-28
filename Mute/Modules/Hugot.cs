using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Mute.Modules
{
    public class Hugot
        : BaseModule
    {
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

        [Command("hugot"), Summary("Nothing, I will ignore all !Hugot commands")]
        public async Task DoNothing(params string[] nothing)
        {
            if (_random.NextDouble() < 0.05f)
                await Context.Message.AddReactionAsync(_reactions[_random.Next(_responses.Length)]);
            else if (_random.NextDouble() <= 0.01f)
                await TypingReplyAsync(_responses[_random.Next(_responses.Length)]);
        }
    }
}
