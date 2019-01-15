using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Mute.Services;

namespace Mute.Modules
{
    public class Introspection
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly UptimeService _uptime;

        public Introspection(DiscordSocketClient client, UptimeService uptime)
        {
            _client = client;
            _uptime = uptime;
        }

        [Command("memory"), RequireOwner, Summary("I will tell you my current memory usage")]
        public async Task MemoryUsage()
        {
            await ReplyAsync(new EmbedBuilder()
                .AddField("Working Set", Environment.WorkingSet.Bytes().Humanize("#.##"), true)
                .AddField("GC Total Memory", GC.GetTotalMemory(false).Bytes().Humanize("#.##"), true)
            );
        }

        [Command("hostinfo"), Summary("I Will tell you where I am being hosted")]
        public async Task HostName()
        {
            var embed = new EmbedBuilder()
                        .AddField("Machine", Environment.MachineName)
                        .AddField("User", Environment.UserName)
                        .AddField("OS", Environment.OSVersion)
                        .AddField("CPUs", Environment.ProcessorCount)
                        .Build();

            await ReplyAsync("", false, embed);
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

        [Command("uptime"), Summary("I will tell you how long I have been running")]
        public async Task Uptime()
        {
            await TypingReplyAsync(_uptime.Uptime.Humanize(2));
        }
    }
}
