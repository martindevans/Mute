using System.Text;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.DiceLang.AST;

public record DiceRollValue(uint Count, uint Sides, uint? ExplodeThreshold)
    : IAstNode
{
    private bool _initialised;
    private readonly List<DiceRollResult> _values = new();

    public double Evaluate(IAstNode.Context context)
    {
        _initialised = true;
        _values.Clear();

        var total = 0ul;
        for (var i = 0u; i < Count; i++)
            total += RollSingle();

        return total;

        ulong RollSingle()
        {
            var value = context.Roller.Roll(Sides);
            _values.Add(new DiceRollResult(value, false));

            if (value >= ExplodeThreshold)
            {
                var explosion = context.Roller.Roll(Sides);
                _values.Add(new DiceRollResult(explosion, true));
                value += explosion;
            }

            return value;
        }
    }

    public IAstNode Reduce()
    {
        return this;
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
                builder.Append('+');

            builder.Append(result.Value);

            start = false;
        }

        return builder.ToString();
    }
}

public record DiceRollResult(ulong Value, bool Explosion);