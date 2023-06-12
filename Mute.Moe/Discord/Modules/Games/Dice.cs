using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.Randomness;
using Pegasus.Common;

namespace Mute.Moe.Discord.Modules.Games;

[UsedImplicitly]
[HelpGroup("games")]
public class Dice
    : BaseModule
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
        "Very doubtful.",
    };

    public Dice(IDiceRoller dice)
    {
        _dice = dice;
    }

    [Command("roll"), Alias("dice"), Summary("I will roll a dice")]
    public async Task RollCmd([Remainder] string command)
    {
        command = command.ToLowerInvariant();

        // Try to parse the command as a number, if that succeeds roll a D(Number)
        if (ulong.TryParse(command, out var sides))
        {
            await TypingReplyAsync(Roll(1, sides));
            return;
        }

        // Try to parse the command as `XdY`. e.g. 3d7
        if (!await TryParseRolls(command))
            await TypingReplyAsync("Sorry I'm not sure what you mean, use something like 3d7 (max 255 dice with 255 sides)");
    }

    [Command("roll2")]
    private async Task Roll2([Remainder] string command)
    {
        try
        {
            var parser = new DiceLangParser { DiceRoller = _dice };
            var result = parser.Parse(command);
            await TypingReplyAsync(result.ToString(CultureInfo.InvariantCulture));
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;
            var m = e.Message;

            var spaces = new string(' ', Math.Max(0, c.Column - 2));
            var err = $"{c.Subject}\n"
                    + $"{spaces}^ {m} (Ln{c.Line}, Col{c.Column - 1})\n";
            await TypingReplyAsync(err);

            await TypingReplyAsync("Sorry but that doesn't seem to be a valid dice command, use something like `3d7`");
        }
    }

    private async Task<bool> TryParseRolls(string command)
    {
        if (!command.Contains('d'))
            return false;
        
        var parts = command.Split('d');
        if (parts.Length != 2)
            return false;
        
        var count = (ushort)1;
        if (!string.IsNullOrWhiteSpace(parts[0]))
            if (!ushort.TryParse(parts[0], out count))
                return false;

        if (!ulong.TryParse(parts[1], out var max))
            return false;

        await TypingReplyAsync(Roll(count, max));
        return true;
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
        return _dice.Flip() ? "Heads" : "Tails";
    }

    private string Roll(ushort dice, ulong sides)
    {
        var results = Enumerable.Range(0, dice).Select(_ => _dice.Roll(sides)).ToArray();
        var total = results.Select(a => (int)a).Sum();

        return dice == 1
             ? total.ToString()
             : $"{string.Join('+', results)} = {total}";
    }

    private string Magic8Ball()
    {
        var index = (int)_dice.Roll((ulong)Ball8Replies.Count) - 1;
        return Ball8Replies[index];
    }
}