using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Exponent(IAstNode Left, IAstNode Right)
    : IAstNode
{
    public async Task<double> Evaluate(IAstNode.Context context) => Math.Pow(await Left.Evaluate(context), await Right.Evaluate(context));

    public override string ToString() => $"{Left} ^ {Right}";
}