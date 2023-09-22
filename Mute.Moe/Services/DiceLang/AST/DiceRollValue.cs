using System.Text;
using System.Threading.Tasks;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.DiceLang.AST;

public record DiceRollValue(IAstNode Count, IAstNode Sides, IAstNode? ExplodeThreshold)
    : IAstNode
{
    private bool _initialised;
    private readonly List<DiceRollResult> _values = new();

    public async Task<double> Evaluate(IAstNode.Context context)
    {
        _initialised = true;
        _values.Clear();

        var count = (ulong)await Count.Evaluate(context);
        var sides = (ulong)await Sides.Evaluate(context);
        var explodeTask = ExplodeThreshold?.Evaluate(context);
        var explode = (ulong?)(explodeTask == null ? default(double?) : await explodeTask);

        var total = 0ul;
        for (var i = 0u; i < count; i++)
            total += RollSingle();

        return total;

        ulong RollSingle()
        {
            var value = context.Roller.Roll(sides);
            _values.Add(new DiceRollResult(value, false));

            if (value >= explode)
            {
                var explosion = context.Roller.Roll(sides);
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
            if (ExplodeThreshold != null)
                return $"{Count}d{Sides}E{ExplodeThreshold}";
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