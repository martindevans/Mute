using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;

namespace Mute.Moe.Discord.Modules
{
    public class Commands
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public Commands(DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        [Command("commands"), Summary("I will respond with a list of commands that I understand"), Alias("help")]
        public async Task ListCommands([NotNull, Remainder] string filter = "")
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

            await DisplayItemList(
                await FindCommands(FilterCommand),
                () => $"Can't find any command which match filter `{filter}`",
                async c => await TypingReplyAsync(FormatCommandDetails(c, _client.CurrentUser)),
                l => {
                    const string suffix = "By the way you can use `!command $name` to get the details of a specific command";
                    if (string.IsNullOrWhiteSpace(filter))
                        return $"There are {l.Count} available commands. " + suffix;
                    else
                        return $"{l.Count} commands matched the search term `{filter}`. " + suffix;
                },
                (c, i) => $"{i + 1}. {FormatCommandSummary(c)}"
            );
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

            await DisplayItemList(
                await FindCommands(FilterCommand),
                () => $"Can't find any command which match filter `{filter}`",
                async c => await TypingReplyAsync(FormatCommandDetails(c, _client.CurrentUser)),
                l => $"{l.Count} commands matched the search term",
                (c, i) => $"{i}. {FormatCommandSummary(c)}"
            );
        }

        #region static helpers
        [NotNull] private static string FormatCommandSummary([NotNull] CommandInfo cmd)
        {
            return $"{FormatCommandName(cmd)} - {cmd.Summary}";
        }

        [NotNull] private static EmbedBuilder FormatCommandDetails([NotNull] CommandInfo cmd, [CanBeNull] IUser self)
        {
            var embed = new EmbedBuilder()
                .WithTitle(FormatCommandName(cmd))
                .WithDescription(cmd.Summary);

            if (self != null)
                embed = embed.WithAuthor(self);

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
                ? $"`!{cmd.Aliases.Single().ToLowerInvariant()}`"
                : $"`!({string.Join('/', cmd.Aliases.Select(a => a.ToLowerInvariant()))})`";
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
