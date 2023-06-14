namespace Mute.Moe.Services.DiceLang.AST;

public record Parameter(string Name)
    : IAstNode
{
    public double Evaluate(IAstNode.Context context)
    {
        var arg = context.NamedArgs[Name];
        return arg.Evaluate(context);
    }

    public IAstNode Reduce()
    {
        return this;
    }

    public override string ToString()
    {
        return Name;
    }
}