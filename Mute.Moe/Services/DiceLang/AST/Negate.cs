using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

public record Negate(IAstNode Inner)
    : IAstNode
{
    public double Evaluate(IDiceRoller roller) => -Inner.Evaluate(roller);

    public override string ToString() => $"-{Inner}";
}