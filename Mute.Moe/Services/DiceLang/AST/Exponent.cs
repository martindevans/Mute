using System;
using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

public record Exponent(IAstNode Left, IAstNode Right)
    : IAstNode
{
    public double Evaluate(IDiceRoller roller) => Math.Pow(Left.Evaluate(roller), Right.Evaluate(roller));

    public override string ToString() => $"{Left} ^ {Right}";
}