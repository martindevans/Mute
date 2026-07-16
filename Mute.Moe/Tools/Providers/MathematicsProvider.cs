using HandyAgentFramework;
using Mute.Moe.Services.DiceLang;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Services.DiceLang.Macros;
using Mute.Moe.Services.Randomness;
using Pegasus.Common;
using System.Threading.Tasks;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provide dice/randomness related tools
/// </summary>
public class MathematicsProvider
    : IToolProvider
{
    private readonly IDiceRoller _dice;

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> Tools { get; }

    /// <summary>
    /// Construct <see cref="MathematicsProvider"/>
    /// </summary>
    /// <param name="dice"></param>
    public MathematicsProvider(IDiceRoller dice)
    {
        _dice = dice;

        Tools =
        [
            new DocStringTool(ToolGroups.Info.Mathematics, "evaluate_expression", EvaluateExpression),
        ];
    }

    /// <summary>
    /// Calculate the result of a mathematical expression (e.g. `9 + 12 ^ 3`). This expression can include arithmetic and dice rolls. Use standard
    /// dice syntax like 7D6E3 to indicate: roll seven six sided dice, explode on a 3 or more. For example: `3 + 4d6 * 1d6E5`.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private async Task<object> EvaluateExpression(string expression)
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