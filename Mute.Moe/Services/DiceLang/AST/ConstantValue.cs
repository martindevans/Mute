using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// Represents a constant numeric value
/// </summary>
/// <param name="Value"></param>
public record ConstantValue(double Value)
    : IAstNode
{
    /// <inheritdoc />
    public async Task<double> Evaluate(IAstNode.Context context) => Value;

    /// <inheritdoc />
    public override string ToString() => $"{Value}";
}