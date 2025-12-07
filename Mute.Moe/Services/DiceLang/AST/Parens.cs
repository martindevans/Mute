using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// Represents (Inner)
/// </summary>
/// <param name="Inner"></param>
public record Parens(IAstNode Inner)
    : IAstNode
{
    /// <inheritdoc />
    public Task<double> Evaluate(IAstNode.Context context) => Inner.Evaluate(context);

    /// <inheritdoc />
    public override string ToString() => $"({Inner})";
}