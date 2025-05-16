using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Randomness;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Games;

[UsedImplicitly]
[HelpGroup("games")]
public class Flip
    : BaseModule
{
    private readonly IDiceRoller _dice;

    private static readonly IReadOnlyList<string> Ball8Replies =
    [
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
    ];


    public Flip(IDiceRoller dice)
    {
        _dice = dice;
    }

    [Command("flip"), Summary("I will flip a coin")]
    [UsedImplicitly]
    public async Task FlipCmd()
    {
        await TypingReplyAsync(_dice.Flip() ? "Heads" : "Tails");
    }

    [Command("8ball"), Summary("I will reach into the hazy mists of the future to determine the truth")]
    [UsedImplicitly]
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