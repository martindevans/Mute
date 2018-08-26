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

        public bool RequiresMention => false;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy", "こんにちは"
        };

        private const float GreetingResponseChance = 0.25f;

        public HelloResponse(DiscordSocketClient client, Random random)
        {
            _client = client;
            _random = random;
        }

        public bool MayRespond([NotNull] IMessage message, bool containsMention)
        {
            var isGreeting = message.Content.Split(' ')
                                    .Select(w => w.ToLowerInvariant())
                                    .Any(_greetings.Contains);

            var direct = ((IUserMessage)message).MentionedUserIds.Contains(_client.CurrentUser.Id);
            return isGreeting && (direct || _random.NextDouble() < GreetingResponseChance);
        }

        public Task<string> Respond([NotNull] IMessage message, bool containsMention, CancellationToken ct)
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
