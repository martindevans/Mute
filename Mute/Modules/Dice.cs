using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Modules
{
    public class Dice
        : ModuleBase
    {
        private readonly RNGCryptoServiceProvider _random = new RNGCryptoServiceProvider();

        private static readonly IReadOnlyList<string> Ball8Replies = new[] {
            "It is certain.",
            "It is decidedly so.",
            "Without a doubt.",
            "Yes - definitely.",
            "You may rely on it.",
            "As I see it, yes.",
            "Most likely.",
            "Outlook good.",
            "Yes.",
            "Signs point to yes.",
            "Reply hazy, try again",
            "Ask again later.",
            "Better not tell you now.",
            "Cannot predict now.",
            "Concentrate and ask again.",
            "Don't count on it.",
            "My reply is no.",
            "My sources say no",
            "Outlook not so good.",
            "Very doubtful."

        };

        private byte Random(byte numberSides)
        {
            bool IsFairRoll(byte roll)
            {
                // There are MaxValue / numSides full sets of numbers that can come up
                // in a single byte.  For instance, if we have a 6 sided die, there are
                // 42 full sets of 1-6 that come up.  The 43rd set is incomplete.
                var fullSetsOfValues = byte.MaxValue / numberSides;

                // If the roll is within this range of fair values, then we let it continue.
                // In the 6 sided die case, a roll between 0 and 251 is allowed.  (We use
                // < rather than <= since the = portion allows through an extra 0 value).
                // 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair
                // to use.
                return roll < numberSides * fullSetsOfValues;
            }

            if (numberSides == 0)
                throw new ArgumentOutOfRangeException(nameof(numberSides));

            // Create a byte array to hold the random value.
            var randomNumber = new byte[1];

            //Keep re-reolling until the roll is fair.
            do
            {
                _random.GetBytes(randomNumber);
            }
            while (!IsFairRoll(randomNumber[0]));

            // Return the random number mod the number
            // of sides.  The possible values are zero-
            // based, so we add one.
            return (byte)((randomNumber[0] % numberSides) + 1);
        }

        [Command("roll"), Alias("dice"), Summary("I will roll a dice")]
        public async Task Roll(byte max = 6)
        {
            var value = Random(max);

            await this.TypingReplyAsync(value.ToString());
        }

        [Command("flip"), Summary("I will flip a coin")]
        public async Task Flip()
        {
            var r = Random(2);

            if (r == 1)
                await this.TypingReplyAsync("Heads");
            else
                await this.TypingReplyAsync("Tails");
        }

        [Command("8ball"), Summary("I will reach into the hazy mists of the future to determine the truth")]
        public async Task Magic8Ball([CanBeNull] params string[] question)
        {
            if (question == null || question.Length == 0)
            {
                await this.TypingReplyAsync("You must ask a question");
                return;
            }

            var index = Random(20) - 1;
            var choice = Ball8Replies[index];
            await this.TypingReplyAsync(choice);
        }
    }
}
