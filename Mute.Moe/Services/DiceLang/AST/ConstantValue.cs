using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record ConstantValue(double Value)
    : IAstNode
{
    public async Task<double> Evaluate(IAstNode.Context context) => Value;

    public override string ToString() => $"{Value}";

    public IAstNode Reduce()
    {
        return this;
    }
}