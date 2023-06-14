namespace Mute.Moe.Services.DiceLang.AST;

public record ConstantValue(double Value)
    : IAstNode
{
    public double Evaluate(IAstNode.Context context) => Value;

    public override string ToString() => $"{Value}";

    public IAstNode Reduce()
    {
        return this;
    }
}