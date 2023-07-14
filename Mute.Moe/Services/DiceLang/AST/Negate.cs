using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Negate(IAstNode Inner)
    : IAstNode
{
    public async Task<double> Evaluate(IAstNode.Context context) => -await Inner.Evaluate(context);

    public override string ToString() => $"-{Inner}";
}