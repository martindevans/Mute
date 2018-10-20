using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
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
        [Alias("help")]
        public async Task Commands([NotNull, Remainder] string filter = "")
        {
            var filters = filter.Split(' ').Select(a => a.ToLowerInvariant()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            bool FilterCommand(CommandInfo command)
            {
                bool Filter(string a) => filters.Any(f => a.ToLowerInvariant().Contains(f));

                if (filters.Length == 0)
                    return true;

                //Does the name pass a filter?
                if (Filter(command.Name))
                    return true;

                //Do any aliases pass a filter?
                if (command.Aliases.Any(Filter))
                    return true;

                //Does the module name pass a filter?
                if (Filter(command.Module.Name))
                    return true;
            
                //Do any module aliases pass a filter?
                if (command.Module.Aliases.Any(Filter))
                    return true;

                return false;
            }

            async Task<bool> CheckPreconditions(ICommandContext context, CommandInfo command, IServiceProvider services)
            {
                foreach (var precondition in command.Preconditions)
                {
                    if (!(await precondition.CheckPermissionsAsync(Context, command, _services)).IsSuccess)
                        return false;
                }

                return true;
            }

            string FormatCommand(CommandInfo cmd)
            {
                var name = cmd.Aliases.Count == 1 ? $"{ cmd.Name.ToLowerInvariant()}" : $"({string.Join('/', cmd.Aliases.Select(a => a.ToLowerInvariant()))})";
                return $"{name} - {cmd.Summary}";
            }

            //Get all commands that are not explicitly hidden and pass the filter
            var commands = _commands
               .Commands
               .Where(c => !c.Attributes.OfType<HiddenAttribute>().Any())
               .Where(FilterCommand)
               .ToList();

            //Remove commands the user does not have permission to execute
            for (var i = commands.Count - 1; i >= 0; i--)
                if (!await CheckPreconditions(Context, commands[i], _services))
                    commands.RemoveAt(i);

            if (commands.Count == 0)
                await this.TypingReplyAsync($"Can't find any command which match filter `{filter}`");

            //Now print all the remaining commands (in batches of limited characters, to ensure we don't exceed the 2000 character limit)
            var builder = new StringBuilder();
            while (commands.Count > 0)
            {
                builder.Append(FormatCommand(commands[0]));
                builder.Append('\n');
                commands.RemoveAt(0);

                if (builder.Length > 1000)
                {
                    await this.TypingReplyAsync(builder.ToString());
                    builder.Clear();
                }
            }

            if (builder.Length > 0)
                await this.TypingReplyAsync(builder.ToString());
        }

        [Command("home"), Summary("I will tell you where to find my source code")]
        public async Task Home()
        {
            await this.TypingReplyAsync("My code is here: https://github.com/martindevans/Mute");
        }

        [Command("shard"), Summary("I will tell you what shard ID I have")]
        public async Task Shard()
        {
            await this.TypingReplyAsync($"Hello from shard {_client.ShardId}");
        }
    }
}
