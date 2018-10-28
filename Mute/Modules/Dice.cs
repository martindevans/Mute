using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class Dice
        : BaseModule, IKeyProvider
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
                return 0;

            // Create a byte array to hold the random value.
            var randomNumber = new byte[1];

            lock (_random)
            {
                //Keep re-reolling until the roll is fair.
                do
                {
                    _random.GetBytes(randomNumber);
                } while (!IsFairRoll(randomNumber[0]));

                // Return the random number mod the number
                // of sides.  The possible values are zero-
                // based, so we add one.
                return (byte)((randomNumber[0] % numberSides) + 1);
            }
        }

        [Command("roll"), Alias("dice"), Summary("I will roll a dice")]
        public async Task RollCmd(string command)
        {
            if (byte.TryParse(command, out var byt))
            {
                await TypingReplyAsync(Roll(1, byt));
                return;
            }

            if (command.Contains("d"))
            {
                var parts = command.Split('d');
                if (parts.Length == 2)
                {
                    if (byte.TryParse(parts[0], out var count) && byte.TryParse(parts[1], out var max))
                    {
                        await TypingReplyAsync(Roll(count, max));
                        return;
                    }
                }
            }

            await TypingReplyAsync("Sorry I'm not sure what you mean, use something like 3d7 (max 255 dice with 255 sides)");
        }

        [Command("flip"), Summary("I will flip a coin")]
        public async Task FlipCmd()
        {
            await TypingReplyAsync(Flip());
        }

        [Command("8ball"), Summary("I will reach into the hazy mists of the future to determine the truth")]
        public async Task Magic8Ball([CanBeNull] params string[] question)
        {
            if (question == null || question.Length == 0)
            {
                await TypingReplyAsync("You must ask a question");
                return;
            }

            await TypingReplyAsync(Magic8Ball());
        }

        [NotNull] private string Flip()
        {
            var r = Random(2);

            if (r == 1)
                return "Heads";
            else
                return "Tails";
        }

        [CanBeNull] private string Roll(string dice, string sides)
        {
            if (!byte.TryParse(dice, out var pdice))
                return null;
            if (!byte.TryParse(sides, out var psides))
                return null;
            return Roll(pdice, psides);
        }

        [NotNull] private string Roll(byte dice, byte sides)
        {
            var results = Enumerable.Range(0, dice).Select(_ => Random(sides)).ToArray();
            var total = results.Select(a => (int)a).Sum();

            return $"{string.Join('+', results)} = {total}";
        }

        [NotNull] private string Magic8Ball()
        {
            var index = Random(20) - 1;
            return Ball8Replies[index];
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("flip", 10,
                    new Decomposition("*", d => Flip())
                );

                yield return new Key("roll", 10,
                    new Decomposition("*roll #d#", d => Roll(d[1], d[2])),
                    new Decomposition("*roll #d# *", d => Roll(d[1], d[2])),
                    new Decomposition("*roll *#*", d => Roll("1", d[2]))
                );

                yield return new Key("8ball", 10,
                    new Decomposition("*8ball *", d => Magic8Ball())
                );
            }
        }
    }
}
