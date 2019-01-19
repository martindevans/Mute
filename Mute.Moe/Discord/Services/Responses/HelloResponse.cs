using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Services.Responses
{
    public class HelloResponse
        : IResponse
    {
        private readonly Random _random;

        public double BaseChance => 0.25;
        public double MentionedChance => 0;

        private static readonly IReadOnlyList<string> GeneralGreetings = new List<string> {
            "Hello {0}", "Hi", "Hiya {0}", "Heya {0}", "Howdy {0}", "\\o", "o/", "Greetings {0}"
        };

        private static readonly IReadOnlyList<string> MorningGreetings = new List<string> {
            "Good morning {0}", "Morning"
        };

        private static readonly IReadOnlyList<string> EveningGreetings = new List<string> {
            "Good evening {0}", "Evening"
        };

        private static readonly IReadOnlyList<string> AllGreetings = GeneralGreetings.Concat(MorningGreetings).Concat(EveningGreetings).Select(a => string.Format(a, "").Trim().ToLowerInvariant()).ToArray();

        public HelloResponse(Random random)
        {
            _random = random;
        }

        public Task<IConversation> TryRespond(MuteCommandContext context, bool containsMention)
        {
            //Determine if thie message is a greeting
            var isGreeting = context.Message.Content.Split(' ').Select(CleanWord).Any(AllGreetings.Contains);

            var gu = (SocketGuildUser)context.User;
            var name = gu.Nickname ?? gu.Username;

            return Task.FromResult<IConversation>(isGreeting
                ? new HelloConversation(string.Format(ChooseGreeting(), name))
                : null
            );
        }

        private string ChooseGreeting()
        {
            var hour = DateTime.UtcNow.Hour;

            if (hour > 5 && hour <= 12 && _random.NextDouble() < 0.25f)
                return MorningGreetings.Random(_random);

            if (hour > 18 && hour <= 24 && _random.NextDouble() < 0.25f)
                return EveningGreetings.Random(_random);

            return GeneralGreetings.Random(_random);
        }

        [NotNull] private static string CleanWord([NotNull] string word)
        {
            return new string(word
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
}
