using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Extensions;

namespace Mute.Modules
{
    public class Introspection
        : ModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly Random _random;

        public Introspection(DiscordSocketClient client, CommandService commands, IServiceProvider services, Random random)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _random = random;
        }

        [Command("ping"), Summary("I will respond with 'pong'")]
        [Alias("test")]
        public async Task Ping()
        {
            await ReplyAsync("pong");
        }

        [Command("latency"), Summary("I wil respond with the server latency")]
        public async Task Latency()
        {
            var latency = _client.Latency;

            if (latency < 75)
                await this.TypingReplyAsync($"My latency is {_client.Latency}ms, that's great!");
            else if (latency < 150)
                await this.TypingReplyAsync($"My latency is {_client.Latency}ms");
            else
                await this.TypingReplyAsync($"My latency is {_client.Latency}ms, that's a bit slow");
        }

        [Command("commands"), Summary("I will respond with a list of commands that I understand")]
        public async Task Commands()
        {
            foreach (var command in _commands.Commands)
            {
                //Skip explicitly hidden commands
                if (command.Attributes.OfType<HiddenAttribute>().Any())
                    continue;

                // Skip commands the user cannot execute
                var preconditionSuccess = true;
                foreach (var precondition in command.Preconditions)
                {
                    preconditionSuccess &= (await precondition.CheckPermissions(Context, command, _services)).IsSuccess;
                    if (!preconditionSuccess)
                        break;
                }
                if (!preconditionSuccess)
                    continue;

                // Show this command
                var name = command.Aliases.Count == 1 ? $"{ command.Name.ToLowerInvariant()}" : $"({string.Join('/', command.Aliases.Select(a => a.ToLowerInvariant()))})";
                await this.TypingReplyAsync($"{name} - {command.Summary}");

                //Add a small delay between each command
                await Task.Delay(_random.Next(300) + 150);
            }
        }

        [Command("home")]
        public async Task Home()
        {
            await this.TypingReplyAsync("My code is here: https://github.com/martindevans/Mute");
        }

        [Command("say")]
        [RequireOwner]
        public async Task Say(params string[] s)
        {
            await this.TypingReplyAsync(string.Join(' ', s));
        }

        [Command("music")]
        [RequireOwner]
        public async Task JoinVoice()
        {
            if (Context.User is IVoiceState v)
            {
                using (var client = await v.VoiceChannel.ConnectAsync())
                {
                    await client.SetSpeakingAsync(true);

                    Console.WriteLine("Starting writing");

                    using (var bytes = File.OpenRead("piano.pcm"))
                    {
                        var discord = client.CreatePCMStream(AudioApplication.Mixed, 90000, 200);
                        await bytes.CopyToAsync(discord);
                        await discord.FlushAsync();
                    }

                    await Task.Delay(1000);

                    await client.SetSpeakingAsync(false);
                }
            }
        }
    }
}
