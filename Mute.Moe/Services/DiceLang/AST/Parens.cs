using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Parens(IAstNode Inner)
    : IAstNode
{
    public Task<double> Evaluate(IAstNode.Context context) => Inner.Evaluate(context);

    public override string ToString() => $"({Inner})";
}