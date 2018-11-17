using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace Mute.Modules
{
    public class Introspection
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public Introspection(DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
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

        [Command("commands"), Summary("I will respond with a list of commands that I understand"), Alias("help")]
        public async Task Commands([NotNull, Remainder] string filter = "")
        {
            //canonicalize filters (split, lower case, remove nulls)
            var filters = filter.Split(' ').Select(a => a.ToLowerInvariant()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            //Match a command as _broadly as possible_
            bool FilterCommand(CommandInfo command)
            {
                bool Filter(string a) => filters.Any(f => a.ToLowerInvariant().Contains(f));

                if (filters.Length == 0)
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

            //Find commands which pass that filter and which the user asking is allowed to execute
            var commands = await FindCommands(FilterCommand);

            if (commands.Count == 0)
                await TypingReplyAsync($"Can't find any command which match filter `{filter}`");
            else if (commands.Count == 1)
                await TypingReplyAsync(FormatCommandDetails(commands.Single()));
            else
                await CommandSummaries(commands, "By the way you can use `!command $name` to get the details of a specific command");
        }

        [Command("command"), Summary("I will give you the details of a specific command")]
        public async Task Command([NotNull, Remainder] string filter = "")
        {
            //canonicalize filters (split, lower case, remove nulls)
            var filters = filter.Split(' ').Select(a => a.ToLowerInvariant()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            //Match a command as _broadly as possible_
            bool FilterCommand(CommandInfo command)
            {
                bool Filter(string a) => filters.Any(f => a.ToLowerInvariant().Equals(f));

                if (filters.Length == 0)
                    return false;

                //Do any aliases pass a filter?
                if (command.Aliases.Any(Filter))
                    return true;

                return false;
            }

            //Find commands which pass that filter and which the user asking is allowed to execute
            var commands = await FindCommands(FilterCommand);

            if (commands.Count == 0)
                await TypingReplyAsync($"Can't find any command which match filter `{filter}`");
            else if (commands.Count == 1)
                await TypingReplyAsync(FormatCommandDetails(commands.Single()));
            else
                await CommandSummaries(commands, $"{commands.Count} commands matched the search term");
        }

        #region static helpers
        private async Task CommandSummaries([NotNull] IEnumerable<CommandInfo> commands, [CanBeNull] string hint)
        {
            var cmds = commands.ToList();

            if (!string.IsNullOrWhiteSpace(hint))
                await TypingReplyAsync(hint);

            //Now print all the remaining commands (in batches of limited characters, to ensure we don't exceed the 2000 character limit)
            var builder = new StringBuilder();
            while (cmds.Count > 0)
            {
                builder.Append(FormatCommandSummary(cmds[0]));
                builder.Append('\n');
                cmds.RemoveAt(0);

                if (builder.Length >= 1000)
                {
                    await TypingReplyAsync(builder.ToString());
                    builder.Clear();
                }
            }

            if (builder.Length > 0)
                await TypingReplyAsync(builder.ToString());
        }

        [NotNull] private static string FormatCommandSummary([NotNull] CommandInfo cmd)
        {
            return $"{FormatCommandName(cmd)} - {cmd.Summary}";
        }

        [NotNull] private static EmbedBuilder FormatCommandDetails([NotNull] CommandInfo cmd)
        {
            var embed = new EmbedBuilder()
                .WithTitle(FormatCommandName(cmd))
                .WithDescription(cmd.Summary);

            foreach (var parameter in cmd.Parameters)
            {
                var name = parameter.Type.Name;
                if (parameter.IsOptional)
                    name += " (optional)";

                var description = parameter.Summary ?? parameter.Name;
                if (parameter.IsOptional)
                    description += $" (default=`{parameter.DefaultValue}`)";

                embed.AddField($"{name}", $"{description}");
            }

            return embed;
        }

        private async Task<bool> CheckCommandPreconditions([NotNull] CommandInfo command, [NotNull] ICommandContext context, [NotNull] IServiceProvider services)
        {
            foreach (var precondition in command.Preconditions)
                if (!(await precondition.CheckPermissionsAsync(Context, command, _services)).IsSuccess)
                    return false;

            return true;
        }

        [NotNull] private static string FormatCommandName([NotNull] CommandInfo cmd)
        {
            return cmd.Aliases.Count == 1
                ? $"!{cmd.Aliases.Single().ToLowerInvariant()}"
                : $"!({string.Join('/', cmd.Aliases.Select(a => a.ToLowerInvariant()))})";
        }

        [ItemNotNull] private async Task<IReadOnlyList<CommandInfo>> FindCommands([NotNull] Func<CommandInfo, bool> filter)
        {
            //Get all commands that are not explicitly hidden and pass the filter
            var commands = _commands
                           .Commands
                           .Where(c => !c.Attributes.OfType<HiddenAttribute>().Any())
                           .Where(filter)
                           .ToList();

            //Remove commands the user does not have permission to execute
            for (var i = commands.Count - 1; i >= 0; i--)
                if (!await CheckCommandPreconditions(commands[i], Context, _services))
                    commands.RemoveAt(i);

            return commands;
        }
        #endregion
    }
}
