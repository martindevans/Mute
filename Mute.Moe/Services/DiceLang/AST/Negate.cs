namespace Mute.Moe.Services.DiceLang.AST;

public record Negate(IAstNode Inner)
    : IAstNode
{
    public double Evaluate(IAstNode.Context context) => -Inner.Evaluate(context);

    public override string ToString() => $"-{Inner}";

    public IAstNode Reduce()
    {
        var i = Inner.Reduce();

        if (i is ConstantValue iv)
            return new ConstantValue(-iv.Value);
        return new Negate(i);
    }
}