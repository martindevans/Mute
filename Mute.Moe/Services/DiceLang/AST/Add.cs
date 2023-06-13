using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

public record Add(IAstNode Left, IAstNode Right)
    : IAstNode
{
    public double Evaluate(IDiceRoller roller) => Left.Evaluate(roller) + Right.Evaluate(roller);

    public override string ToString() => $"{Left} + {Right}";
}