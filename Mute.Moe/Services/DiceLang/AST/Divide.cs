using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// Represents Left / Right
/// </summary>
/// <param name="Left"></param>
/// <param name="Right"></param>
public record Divide(IAstNode Left, IAstNode Right)
    : IAstNode
{
    /// <inheritdoc />
    public async Task<double> Evaluate(IAstNode.Context context) => await Left.Evaluate(context) / await Right.Evaluate(context);

    /// <inheritdoc />
    public override string ToString() => $"{Left} / {Right}";
}