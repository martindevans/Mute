using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

public record ConstantValue(double Value)
    : IAstNode
{
    public double Evaluate(IDiceRoller _) => Value;

    public override string ToString() => $"{Value}";
}