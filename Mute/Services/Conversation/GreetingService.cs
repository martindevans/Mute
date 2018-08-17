using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services.Conversation
{
    public class GreetingService
    {
        [NotNull] private readonly DiscordSocketClient _client;
        [NotNull] private readonly Random _random;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy", "こんにちは"
        };

        private const float GreetingResponseChance = 0.05f;

        public GreetingService([NotNull] DiscordSocketClient client)
        {
            _client = client;
            _random = new Random();

            client.MessageReceived += OnMessage;
            client.UserJoined += OnJoined;
            client.UserLeft += OnLeft;
        }

        //Get `the-war-room` channel in `lightbuild appreciation society`
        private ISocketMessageChannel Channel => (ISocketMessageChannel)_client.GetGuild(415655090842763265).GetChannel(415655091463389194);

        private async Task OnLeft([NotNull] SocketGuildUser a)
        {
            await Channel.SendMessageAsync($"Goodbye {a.Mention}");
        }

        private async Task OnJoined([NotNull] SocketGuildUser a)
        {
            var greeting = RandomGreeting();
            await Channel.SendMessageAsync($"{greeting} {a.Nickname}");
        }

        private async Task OnMessage([NotNull] SocketMessage msg)
        {
            if (msg.Author.Id == _client.CurrentUser.Id)
                return;

            var isGreeting = await Task.Factory.StartNew(() => msg.Content.Split(' ').Select(w => w.ToLowerInvariant()).Any(_greetings.Contains));
            var direct = ((IUserMessage)msg).MentionedUserIds.Contains(_client.CurrentUser.Id);
            if (isGreeting && (direct || _random.NextDouble() < GreetingResponseChance))
            {
                var name = ((SocketGuildUser)msg.Author).Nickname;
                var greeting = RandomGreeting();

                await msg.Channel.SendMessageAsync($"{greeting} {name}");
            }
        }

        [NotNull] private string RandomGreeting()
        {
            var g = _greetings.Random(_random);

            return char.ToUpper(g[0]) + g.Substring(1);
        }
    }
}
