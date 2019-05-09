using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    [HelpGroup("help")]
    public class Help
        : BaseModule
    {
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        private readonly char _prefixCharacter;

        public Help(CommandService commands, IServiceProvider services, [NotNull] Configuration config)
        {
            _commands = commands;
            _services = services;

            _prefixCharacter = config.PrefixCharacter;
        }

        [Command("modules"), Alias("help", "commands")]
        [Summary("I will list all command modules (groups of commands)")]
        public async Task ListModules()
        {
            //Find modules which have at least one permitted command
            var modules = await FindModules();

            //Display information
            var embed = new EmbedBuilder()
                .WithDescription($"Use `{_prefixCharacter}help name` to find out about a specific command or module")
                .WithAuthor(Context.Client.CurrentUser);
            foreach (var item in modules)
                embed = embed.AddField(item.Key.Name, string.Join(", ", item.Value.Select(c => $"`{c.Name}`")));
            await ReplyAsync(embed);
        }

        [Command("help"), Summary("I will tell you about commands or modules")]
        public async Task ListDetails([Remainder] string search)
        {
            var commands = await FindCommands(search).ToArray();
            var modules = await FindModules(search).ToArray();

            if (commands[0].Key == 0)
                await ReplyAsync(FormatCommandDetails(Context, _prefixCharacter, commands[0]));
            else if (modules[0].Item1 == 0)
                await ReplyAsync(FormatModuleDetails(Context, _prefixCharacter, modules[0].Item2));
            else
            {
                var items = commands.SelectMany(g => g.Select(c => (g.Key, c.Name))).Concat(modules.Select(m => (m.Item1, m.Item2.Name)))
                                    .DistinctBy(a => a.Name)
                                    .OrderBy(a => a.Item1);
                await ReplyAsync("I can't find a module or command with that name, did you mean one of these: " + string.Join(", ", items.Take(10).Select(m => $"`{m.Name.ToLower()}`")));
            }
        }

        [Command("module")]
        [Summary("I will tell you about the commands in a specific module")]
        public async Task ListModuleDetails([Remainder] string search)
        {
            var modules = await FindModules(search).ToArray();
            if (modules[0].Item1 == 0)
                await ReplyAsync(FormatModuleDetails(Context, _prefixCharacter, modules[0].Item2));
            else
                await ReplyAsync("I can't find a module with that name, did you mean one of these: " + string.Join(", ", modules.Take(10).Select(m => $"`{m.Item2.Name.ToLower()}`")));
        }

        [Command("command")]
        [Summary("I will tell you about a specific command")]
        public async Task ListCommandDetails([Remainder] string name)
        {
            var commands = await FindCommands(name).ToArray();
            if (commands[0].Key == 0)
            {
                await ReplyAsync(FormatCommandDetails(Context, _prefixCharacter, commands[0]));
            }
            else
            {
                var cs = commands.SelectMany(g => g.Select(c => (g.Key, c.Name))).DistinctBy(c => c.Name);
                await ReplyAsync("I can't find a command with that name, did you mean one of these: " + string.Join(", ", cs.Take(10).Select(c => $"`{c.Name.ToLower()}`")));
            }
        }

        [ItemNotNull] private async Task<IEnumerable<IGrouping<uint, CommandInfo>>> FindCommands(string search)
        {
            return from kvp in await FindModules()
                   let module = kvp.Key
                   from command in kvp.Value
                   from alias in command.Aliases.Append(command.Name)
                   let distance = alias.Levenshtein(search)
                   group command by distance
                   into grp
                   orderby grp.Key
                   select grp;
        }

        [ItemNotNull] private async Task<IEnumerable<(uint, ModuleInfo, IReadOnlyList<CommandInfo>)>> FindModules(string search)
        {
            //Find modules with at least one command we can execute, ordered by levenshtein distance to name or alias
            return from moduleKvp in await FindModules()
                   let module = moduleKvp.Key
                   from moduleName in module.Aliases.Append(module.Name).Concat(module.Attributes.OfType<HelpGroupAttribute>().Select(g => g.GroupId))
                   where !string.IsNullOrEmpty(moduleName)
                   let nameLev = moduleName.ToLower().Levenshtein(search.ToLower())
                   orderby nameLev
                   select (nameLev, module, moduleKvp.Value);
        }

        /// <summary>
        /// Find modules which have at least one command the user can execute
        /// </summary>
        [ItemNotNull] private async Task<IReadOnlyDictionary<ModuleInfo, IReadOnlyList<CommandInfo>>> FindModules()
        {
            //Find non hidden modules
            var modules = _commands
                .Modules
                .Where(m => !m.Attributes.OfType<HiddenAttribute>().Any());

            //Filter to modules which have commands we are allowed to execute
            var output = new Dictionary<ModuleInfo, IReadOnlyList<CommandInfo>>();
            foreach (var module in modules)
            {
                var commands = new List<CommandInfo>();
                foreach (var cmd in module.Commands)
                    if (await cmd.CheckCommandPreconditions(Context, _services))
                        commands.Add(cmd);

                if (commands.Any())
                    output.Add(module, commands);
            }

            return output;
        }

        [NotNull] private static EmbedBuilder CreateEmbed([NotNull] ICommandContext context, char prefix, string title, string description)
        {
            return new EmbedBuilder()
                .WithAuthor(context.Client.CurrentUser)
                .WithFooter($"Use `{prefix}help name` to find out about a specific command or module")
                .WithTitle(title)
                .WithDescription(description);
        }

        [NotNull] private static string FormatCommandName([NotNull] CommandInfo cmd, char prefix)
        {
            return cmd.Aliases.Count == 1
                 ? $"`{prefix}{cmd.Aliases[0].ToLowerInvariant()}`"
                 : $"`!({string.Join('/', cmd.Aliases.Select(a => a.ToLowerInvariant()))})`";
        }

        [NotNull] public static EmbedBuilder FormatCommandDetails([NotNull] ICommandContext context, char prefix, [NotNull] IEnumerable<CommandInfo> cmds)
        {
            EmbedBuilder SingleCommandDetails(CommandInfo cmd)
            {
                var embed = CreateEmbed(context, prefix, FormatCommandName(cmd, prefix), cmd.Summary ?? cmd.Remarks);

                var example = $"{prefix}{cmd.Aliases[0]} ";
                foreach (var parameterInfo in cmd.Parameters)
                {
                    if (typeof(string).IsAssignableFrom(parameterInfo.Type))
                        example += "\"some text\"";
                    else if (typeof(int).IsAssignableFrom(parameterInfo.Type) || typeof(uint).IsAssignableFrom(parameterInfo.Type) || typeof(long).IsAssignableFrom(parameterInfo.Type))
                        example += 42;
                    else if (typeof(ulong).IsAssignableFrom(parameterInfo.Type))
                        example += 34;
                    else if (typeof(byte).IsAssignableFrom(parameterInfo.Type))
                        example += 17;
                    else if (typeof(IUser).IsAssignableFrom(parameterInfo.Type))
                        example += context.Client.CurrentUser.Mention;
                    else if (typeof(IChannel).IsAssignableFrom(parameterInfo.Type))
                        example += context.Channel.Name;
                    else if (typeof(IRole).IsAssignableFrom(parameterInfo.Type))
                        example += context.Guild?.EveryoneRole.Mention ?? "@rolename";
                    else if (parameterInfo.Type.IsEnum)
                        example += Enum.GetNames(parameterInfo.Type).First();
                    else
                        example += parameterInfo.Type.Name;
                }
                embed.AddField("Example", example);

                foreach (var parameter in cmd.Parameters)
                {
                    var name = parameter.Type.Name;
                    if (parameter.IsOptional)
                        name += " (optional)";

                    var description = $"`{parameter.Name}` {parameter.Summary}";
                    if (parameter.IsOptional)
                        description += $" (default=`{parameter.DefaultValue}`)";

                    embed.AddField($"{name}", $"{description}");
                }

                return embed;
            }

            EmbedBuilder MultiCommandDetails(IReadOnlyCollection<CommandInfo> multi)
            {
                var embed = CreateEmbed(context, prefix, $"{multi.Count} commands", "description");

                foreach (var item in multi)
                    embed.AddField(FormatCommandName(item, prefix), item.Summary ?? item.Remarks ?? $"No description {EmojiLookup.Confused}");

                return embed;
            }

            var cmdArr = cmds.ToArray();
            return cmdArr.Length == 1
                 ? SingleCommandDetails(cmdArr[0])
                 : MultiCommandDetails(cmdArr);
        }

        [NotNull] private static EmbedBuilder FormatModuleDetails([NotNull] ICommandContext context, char prefix, [NotNull] ModuleInfo module)
        {
            return CreateEmbed(context, prefix, module.Name, module.Summary ?? module.Remarks)
                .AddField("Commands:", string.Join(", ", module.Commands.Select(a => FormatCommandName(a, prefix))));
        }
    }
}
