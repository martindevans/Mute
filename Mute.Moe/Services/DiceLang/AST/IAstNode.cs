using System.Collections.Generic;
using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

public interface IAstNode
{
    public double Evaluate(IDiceRoller roller);
}

public record RollResult(IEnumerable<ulong> Values, uint? ExplodeThreshold, IEnumerable<ulong> ExplodeValues);