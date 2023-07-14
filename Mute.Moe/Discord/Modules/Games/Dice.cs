using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Services.DiceLang.Macros;
using Mute.Moe.Services.Randomness;
using Pegasus.Common;

namespace Mute.Moe.Discord.Modules.Games;

[UsedImplicitly]
[HelpGroup("games")]
public class Dice
    : BaseModule
{
    private readonly IDiceRoller _dice;
    private readonly IMacroResolver _macros;

    public Dice(IDiceRoller dice, IMacroResolver macros)
    {
        _dice = dice;
        _macros = macros;
    }

    [Command("roll"), Summary("I will roll a dice, allowing use of complex mathematical expressions")]
    public async Task Roll([Remainder] string command)
    {
        try
        {
            var parser = new DiceLangParser();
            var result = parser.Parse(command);
            var value = await result.Evaluate(_dice, _macros);
            var description = result.ToString();

            await TypingReplyAsync($"{value.ToString(CultureInfo.InvariantCulture)} = {description}");
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;
            var m = e.Message;

            var spaces = new string(' ', Math.Max(0, c.Column - 2));
            var err = $"```{c.Subject}\n{spaces}^ {m}```";
            await TypingReplyAsync(err);

            await TypingReplyAsync("Sorry but that doesn't seem to be a valid dice command, use something like `3d7`");
        }
        catch (MacroNotFoundException e)
        {
            await TypingReplyAsync($"I'm sorry, I couldn't find the macro `{e.Namespace}::{e.Name}` which you tried to use");
        }
        catch (MacroIncorrectArgumentCount e)
        {
            await TypingReplyAsync($"I'm sorry but macro `{e.Namespace}::{e.Name}` expects {e.Expected} parameters, you supplied {e.Actual}");
        }
    }
}

[UsedImplicitly]
[Group("macro")]
public class Macro
    : BaseModule
{
    private readonly IMacroStorage _macros;

    public Macro(IMacroStorage macros)
    {
        _macros = macros;
    }

    [Command("find")]
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
            item => TypingReplyAsync(item.ToString()),
            items => $"There are {items.Count} matching macros:",
            (item, _) => item.ToString()
        );
    }

    [Command("create")]
    public async Task CreateMacro([Remainder] string expression)
    {
        try
        {
            var def = new DiceLangParser().ParseMacroDefinition(expression);

            var macro = await _macros.Find(def.Namespace, def.Name);
            if (macro != null)
            {
                await TypingReplyAsync("Sorry but that macro already exists!");
                return;
            }

            await _macros.Create(def);
            await TypingReplyAsync($"Created new macro: `{def}`");
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;
            var m = e.Message;

            var spaces = new string(' ', Math.Max(0, c.Column - 2));
            var err = $"```{c.Subject}\n{spaces}^ {m}```";
            await TypingReplyAsync(err);
        }
    }

    [Command("delete")]
    [RequireOwner]
    public async Task DeleteMacro(string ns, string name)
    {
        await _macros.Delete(ns, name);
        await TypingReplyAsync("It is done.");
    }
}