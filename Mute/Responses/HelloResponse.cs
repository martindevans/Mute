using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Responses
{
    public class HelloResponse
        : IResponse
    {
        private readonly DiscordSocketClient _client;
        private readonly Random _random;

        private const double ChanceOfIndirectResponse = 0.25;

        public bool RequiresMention => false;
        public double Chance => 1;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy"
        };

        public HelloResponse(DiscordSocketClient client, Random random)
        {
            _client = client;
            _random = random;
        }

        public Task<bool> MayRespond(IMessage message, bool containsMention)
        {
            var isGreeting = message.Content.Split(' ')
                                    .Select(CleanWord)
                                    .Any(_greetings.Contains);

            var direct = ((IUserMessage)message).MentionedUserIds.Contains(_client.CurrentUser.Id);
            return Task.FromResult(isGreeting && (direct || _random.NextDouble() < ChanceOfIndirectResponse));
        }

        [NotNull] private static string CleanWord([NotNull] string word)
        {
            return new string(word
                .ToLowerInvariant()
                .Where(c => !char.IsPunctuation(c))
                .ToArray()
            );
        }

        public Task<string> Respond(IMessage message, bool containsMention, CancellationToken ct)
        {
            var name = ((SocketGuildUser)message.Author).Nickname;
            var greeting = RandomGreeting();

            return Task.FromResult($"{greeting} {name}");
        }

        [NotNull] private string RandomGreeting()
        {
            var g = _greetings.Random(_random);

            return char.ToUpper(g[0]) + g.Substring(1);
        }
    }
}
