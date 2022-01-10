using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Responses.Eliza;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Discord.Modules.Games
{
    [HelpGroup("games")]
    public class Dice
        : BaseModule, IKeyProvider
    {
        private readonly IDiceRoller _dice;

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

        public Dice(IDiceRoller dice)
        {
            _dice = dice;
        }

        [Command("roll"), Alias("dice"), Summary("I will roll a dice")]
        public async Task RollCmd(string command)
        {
            //Try to parse the command as a number, if that succeeds roll a D(Number)
            if (ulong.TryParse(command, out var sides))
            {
                await TypingReplyAsync(Roll(1, sides));
                return;
            }

            //Try to parse the command as `dX dY dZ` and so on. A list of dice to roll
            if (command.Contains("d"))
            {
                var parts = command.Split('d');
                if (parts.Length == 2)
                {
                    if (byte.TryParse(parts[0], out var count) && ulong.TryParse(parts[1], out var max))
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
        public async Task Magic8Ball([Remainder] string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await TypingReplyAsync("You must ask a question");
                return;
            }

            await TypingReplyAsync(Magic8Ball());
        }

        private string Flip()
        {
            if (_dice.Flip())
                return "Heads";
            else
                return "Tails";
        }

        private string? Roll(string dice, string sides)
        {
            if (!byte.TryParse(dice, out var pdice))
                return null;
            if (!byte.TryParse(sides, out var psides))
                return null;
            return Roll(pdice, psides);
        }

        private string Roll(byte dice, ulong sides)
        {
            var results = Enumerable.Range(0, dice).Select(_ => _dice.Roll(sides)).ToArray();
            var total = results.Select(a => (int)a).Sum();

            return $"{string.Join('+', results)} = {total}";
        }

        private string Magic8Ball()
        {
            var index = (int)_dice.Roll((ulong)Ball8Replies.Count) - 1;
            return Ball8Replies[index];
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("flip",
                    new Decomposition("*", d => Flip())
                );

                yield return new Key("roll",
                    new Decomposition("*roll #d#", d => Roll(d[1], d[2])),
                    new Decomposition("*roll #d# *", d => Roll(d[1], d[2])),
                    new Decomposition("*roll *#*", d => Roll("1", d[2]))
                );

                yield return new Key("8ball",
                    new Decomposition("*8ball *", d => Magic8Ball())
                );
            }
        }
    }
}
