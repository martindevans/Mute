namespace Mute.Moe.Services.DiceLang.AST;

public record Subtract(IAstNode Left, IAstNode Right)
    : IAstNode
{
    public double Evaluate(IAstNode.Context context) => Left.Evaluate(context) - Right.Evaluate(context);

    public override string ToString() => $"{Left} - {Right}";

    public IAstNode Reduce() => IAstNode.BinaryReduce(Left, Right, (a, b) => new Subtract(a, b));
}