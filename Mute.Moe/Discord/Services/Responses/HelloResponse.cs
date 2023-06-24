using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Services.Responses;

[UsedImplicitly]
public class HelloResponse
    : IResponse
{
    private readonly Random _random;

    public double BaseChance => 0.25;
    public double MentionedChance => 0;

    private static readonly IReadOnlyList<string> GeneralGreetings = new List<string> {
        "Hello {0}", "Hi", "Hiya {0}", "Heya {0}", "Howdy {0}", "\\o", "o/", "Greetings {0}",
    };

    private static readonly IReadOnlyList<string> MorningGreetings = new List<string> {
        "Good morning {0}",
    };

    private static readonly IReadOnlyList<string> EveningGreetings = new List<string> {
        "Good evening {0}",
    };

    private static readonly IReadOnlyList<string> AllGreetings = GeneralGreetings.Concat(MorningGreetings).Concat(EveningGreetings).Select(a => string.Format(a, "").Trim().ToLowerInvariant()).ToArray();

    public HelloResponse(Random random)
    {
        _random = random;
    }

    public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
    {
        //Determine if thie message is a greeting
        var isGreeting = context.Message.Content.Split(' ').Select(CleanWord).Any(a => AllGreetings.Contains(a));

        var gu = context.User as SocketGuildUser;
        var name = gu?.Nickname ?? context.User.Username;

        return isGreeting
            ? new HelloConversation(string.Format(ChooseGreeting(), name))
            : null;
    }

    private string ChooseGreeting()
    {
        var hour = DateTime.UtcNow.Hour;

        return hour switch {
            > 5 and <= 12 when _random.NextDouble() < 0.25f => MorningGreetings.Random(_random),
            > 18 and <= 24 when _random.NextDouble() < 0.25f => EveningGreetings.Random(_random),
            _ => GeneralGreetings.Random(_random),
        };
    }

    private static string CleanWord(string word)
    {
        return new(word
            .ToLowerInvariant()
            .Trim()
            .Where(c => !char.IsPunctuation(c))
            .ToArray()
        );
    }

    private class HelloConversation
        : TerminalConversation
    {
        public HelloConversation(string response)
            : base(response)
        {
        }
    }
}