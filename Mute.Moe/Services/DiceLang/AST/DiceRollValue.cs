using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Mute.Moe.Services.Randomness;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.DiceLang.AST;

public record DiceRollValue(uint Count, uint Sides, uint? ExplodeThreshold)
    : IAstNode
{
    private bool _initialised;
    private readonly List<DiceRollResult> _values = new();

    public double Evaluate(IDiceRoller roller)
    {
        _initialised = true;
        _values.Clear();

        var total = 0ul;
        for (var i = 0u; i < Count; i++)
            total += RollSingle();

        return total;

        ulong RollSingle()
        {
            var value = roller.Roll(1, Sides);
            _values.Add(new DiceRollResult(value, false));

            if (value >= ExplodeThreshold)
            {
                var explosion = roller.Roll(1, Sides);
                _values.Add(new DiceRollResult(explosion, true));
                value += explosion;
            }

            return value;
        }
    }

    public override string ToString()
    {
        if (!_initialised)
        {
            if (ExplodeThreshold.HasValue)
                return $"{Count}d{Sides}E{ExplodeThreshold.Value}";
            return $"{Count}d{Sides}";
        }

        var builder = new StringBuilder();

        var start = true;
        foreach (var result in _values)
        {
            if (result.Explosion)
                builder.Append(EmojiLookup.Explosion);
            else if (!start)
                builder.Append(',');

            builder.Append(result.Value);

            start = false;
        }

        return builder.ToString();
    }
}

public record DiceRollResult(ulong Value, bool Explosion);