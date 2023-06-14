using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Subtract(IAstNode Left, IAstNode Right)
    : IAstNode
{
    public async Task<double> Evaluate(IAstNode.Context context) => await Left.Evaluate(context) - await Right.Evaluate(context);

    public override string ToString() => $"{Left} - {Right}";
}