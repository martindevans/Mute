using Discord.Interactions;
using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Services.DiceLang.Macros;
using Mute.Moe.Services.Randomness;
using Pegasus.Common;
using System.Globalization;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Interactions.Games;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[Group("dice", "Rolling dice")]
[UsedImplicitly]
public class Dice
    : MuteInteractionModuleBase
{
    private readonly IDiceRoller _dice;
    private readonly IMacroResolver _macros;

    public Dice(IDiceRoller dice, IMacroResolver macros)
    {
        _dice = dice;
        _macros = macros;
    }

    [SlashCommand("roll", "I will roll a dice, allowing use of complex mathematical expressions")]
    [UsedImplicitly]
    public async Task Roll([Summary("command", "e.g. 1d10 + 3d4 * 1d6")] string command)
    {
        try
        {
            var parser = new DiceLangParser();
            var ast = parser.Parse(command);
            var value = await ast.Evaluate(_dice, _macros);
            var description = ast.ToString();

            // If it's a simple roll (1 dice, no explode) just display the value
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (ast is DiceRollValue { ExplodeThreshold: null, Count: ConstantValue { Value: 1 } })
                await RespondAsync($"{value}");
            else
                await RespondAsync($"{value.ToString(CultureInfo.InvariantCulture)} = {description}");
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;
            var m = e.Message;

            var spaces = new string(' ', Math.Max(0, c.Column - 2));
            var err = $"```{c.Subject}\n{spaces}^ {m}```";
            await RespondAsync(err);

            await RespondAsync("Sorry but that doesn't seem to be a valid dice command, use something like `3d7`");
        }
        catch (MacroNotFoundException e)
        {
            await RespondAsync($"I'm sorry, I couldn't find the macro `{e.Namespace}::{e.Name}` which you tried to use");
        }
        catch (MacroIncorrectArgumentCount e)
        {
            await RespondAsync($"I'm sorry but macro `{e.Namespace}::{e.Name}` expects {e.Expected} parameters, you supplied {e.Actual}");
        }
    }

    [Group("macro", "Macros for dice rolling expressions")]
    [UsedImplicitly]
    public class DiceMacro
        : MuteInteractionModuleBase
    {
        private readonly IMacroStorage _macros;

        public DiceMacro(IMacroStorage macros)
        {
            _macros = macros;
        }

        [SlashCommand("find", "Search for existing macros")]
        [UsedImplicitly]
        public async Task FindMacros(string name)
        {
            // Get all results, matching search string with both name and namespace
            var results1 = await _macros.FindAll(null, name).ToListAsync();
            var results2 = await _macros.FindAll(name, null).ToListAsync();
            var results = results1.Concat(results2).DistinctBy(a => a.Namespace + a.Name)
                                  .GroupBy(a => a.Namespace)
                                  .Select(grp => grp.OrderBy(b => b.Name).ToArray())
                                  .SelectMany(a => a)
                                  .ToList();

            // Display all results
            await DisplayItemList(
                results,
                () => "Found no matching macros",
                item => item.ToString(),
                items => $"There are {items.Count} matching macros:",
                (item, _) => item.ToString()
            );
        }

        [SlashCommand("create", "Create a new dice macro")]
        [UsedImplicitly]
        public async Task CreateMacro(string expression)
        {
            try
            {
                var def = new DiceLangParser().ParseMacroDefinition(expression);

                var macro = await _macros.Find(def.Namespace, def.Name);
                if (macro != null)
                {
                    await RespondAsync("Sorry but that macro already exists!");
                }
                else
                {
                    await _macros.Create(def);
                    await RespondAsync($"Created new macro: `{def}`");
                }
            }
            catch (FormatException e)
            {
                var c = (Cursor)e.Data["cursor"]!;
                var m = e.Message;

                var spaces = new string(' ', Math.Max(0, c.Column - 2));
                var err = $"```{c.Subject}\n{spaces}^ {m}```";
                await RespondAsync(err);
            }
        }

        [SlashCommand("delete", "delete a dice roll macro")]
        [RequireOwner]
        [UsedImplicitly]
        public async Task DeleteMacro(string @namespace, string name)
        {
            await _macros.Delete(@namespace, name);
            await RespondAsync("It is done.");
        }
    }
}