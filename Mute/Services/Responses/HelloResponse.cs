using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services.Responses
{
    public class HelloResponse
        : IResponse
    {
        private readonly Random _random;

        public double BaseChance => 0.25;
        public double MentionedChance => 1;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy", "good day"
        };

        public HelloResponse(Random random)
        {
            _random = random;
        }

        public Task<IConversation> TryRespond(IMessage message, bool containsMention)
        {
            return Task.Run<IConversation>(() => {

                //Determine if thie message is a greeting
                var isGreeting = message.Content.Split(' ').Select(CleanWord).Any(_greetings.Contains);

                if (isGreeting)
                    return new HelloConversation($"{RandomGreeting()} {((SocketGuildUser)message.Author).Nickname}");
                else
                    return null;

            });
        }

        [NotNull] private static string CleanWord([NotNull] string word)
        {
            return new string(word
                .ToLowerInvariant()
                .Where(c => !char.IsPunctuation(c))
                .ToArray()
            );
        }

        [NotNull] private string RandomGreeting()
        {
            var g = _greetings.Random(_random);

            return char.ToUpper(g[0]) + g.Substring(1);
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
