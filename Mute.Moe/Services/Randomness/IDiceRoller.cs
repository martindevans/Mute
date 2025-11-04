using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Tools;
using Pegasus.Common;
using System.Threading.Tasks;
using Mute.Moe.Services.DiceLang.Macros;

namespace Mute.Moe.Services.Randomness;

/// <summary>
/// Extension methods for <see cref="IDiceRoller"/>
/// </summary>
public static class IDiceRollerExtensions
{
    /// <summary>
    /// Flip a coin (D2)
    /// </summary>
    /// <param name="dice"></param>
    /// <returns></returns>
    public static bool Flip(this IDiceRoller dice)
    {
        return dice.Roll(2) == 1;
    }
}

/// <summary>
/// Random number generator
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Roll an N sided dice
    /// </summary>
    /// <param name="sides"></param>
    /// <returns></returns>
    ulong Roll(ulong sides);
}

/// <summary>
/// Provide dice/randomness related tools
/// </summary>
public class DiceRollToolProvider
    : IToolProvider
{
    private readonly IDiceRoller _dice;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Construct <see cref="DiceRollToolProvider"/>
    /// </summary>
    /// <param name="dice"></param>
    public DiceRollToolProvider(IDiceRoller dice)
    {
        _dice = dice;

        Tools =
        [
            new AutoTool("math_expression_calculator", false, GetDiceRoll),
        ];
    }

    /// <summary>
    /// Calculate the result of a mathematical expression (e.g. `9 + 12 ^ 3`). This expression can include arithmetic and dice rolls. Use standard dice syntax like
    /// 7D6E3 to indicate: roll 7 six sided dice, explode on a 3 or more. For example: `3 + 4d6 * 1d6E5`.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private async Task<object> GetDiceRoll(string expression)
    {
        try
        {
            var parser = new DiceLangParser();
            var ast = parser.Parse(expression);
            var value = await ast.Evaluate(_dice, new NullMacroResolver());
            var description = ast.ToString();

            return new
            {
                Value = value,
                Description = description,
            };
        }
        catch (FormatException e)
        {
            var c = (Cursor)e.Data["cursor"]!;

            return new
            {
                Error = e.Message,
                Cursor = c.Location
            };
        }
        catch (MacroNotFoundException e)
        {
            return new
            {
                Error = $"Unknown macro/function '{e.Namespace}::{e.Name}'"
            };
        }
        catch (MacroIncorrectArgumentCount e)
        {
            return new
            {
                Error = $"Incorrect argument count to macro/function '{e.Namespace}::{e.Name}'",
                ExpectedCount = e.Expected,
                ActualCount = e.Actual,
            };
        }
    }
}