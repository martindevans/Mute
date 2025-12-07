using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// Represents -Inner
/// </summary>
/// <param name="Inner"></param>
public record Negate(IAstNode Inner)
    : IAstNode
{
    /// <inheritdoc />
    public async Task<double> Evaluate(IAstNode.Context context) => -await Inner.Evaluate(context);

    /// <inheritdoc />
    public override string ToString() => $"-{Inner}";
}