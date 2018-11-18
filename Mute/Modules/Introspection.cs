using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Mute.Modules
{
    public class Introspection
        : BaseModule
    {
        private readonly DiscordSocketClient _client;

        public Introspection(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("ping"), Summary("I will respond with 'pong'"), Alias("test")]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }

        [Command("latency"), Summary("I will respond with the server latency")]
        public async Task Latency()
        {
            var latency = _client.Latency;

            if (latency < 75)
                await TypingReplyAsync($"My latency is {_client.Latency}ms, that's great!");
            else if (latency < 150)
                await TypingReplyAsync($"My latency is {_client.Latency}ms");
            else
                await TypingReplyAsync($"My latency is {_client.Latency}ms, that's a bit slow");
        }

        [Command("home"), Summary("I will tell you where to find my source code"), Alias("source", "github")]
        public async Task Home()
        {
            await TypingReplyAsync("My code is here: https://github.com/martindevans/Mute");
        }

        [Command("shard"), Summary("I will tell you what shard ID I have")]
        public async Task Shard()
        {
            await TypingReplyAsync($"Hello from shard {_client.ShardId}");
        }
    }
}
