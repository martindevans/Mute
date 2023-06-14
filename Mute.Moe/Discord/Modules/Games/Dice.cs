using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.DiceLang.AST;
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

    [Command("roll"), Summary("I will roll a dice, allowing use of complex mathematical expressions")]
    public async Task Roll([Remainder] string command)
    {
        try
        {
            var parser = new DiceLangParser();
            var result = parser.Parse(command);
            var value = result.Evaluate(_dice, new NullMacroResolver());
            var description = result.ToString();

            await TypingReplyAsync($"{value.ToString(CultureInfo.InvariantCulture)} = {description}");
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;
            var m = e.Message;

            var spaces = new string(' ', Math.Max(0, c.Column - 2));
            var err = $"```{c.Subject}\n{spaces}^ {m}```";
            await TypingReplyAsync(err);

            await TypingReplyAsync("Sorry but that doesn't seem to be a valid dice command, use something like `3d7`");
        }
        catch (MacroNotFoundException e)
        {
            await TypingReplyAsync($"I'm sorry, I couldn't find the macro `{e.Namespace}::{e.Name}` which you tried to use");
        }
        catch (MacroIncorrectArgumentCount e)
        {
            await TypingReplyAsync($"I'm sorry but macro `{e.Namespace}::{e.Name}` expects {e.Expected} parameters, you supplied {e.Actual}");
        }
    }

    [Command("flip"), Summary("I will flip a coin")]
    public async Task FlipCmd()
    {
        await TypingReplyAsync(_dice.Flip() ? "Heads" : "Tails");
    }

    [Command("8ball"), Summary("I will reach into the hazy mists of the future to determine the truth")]
    public async Task Magic8Ball([Remainder] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            await TypingReplyAsync("You must ask a question");
            return;
        }

        var index = (int)_dice.Roll((ulong)Ball8Replies.Count) - 1;
        var reply = Ball8Replies[index];

        await TypingReplyAsync(reply);
    }
}