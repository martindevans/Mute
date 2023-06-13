

namespace Mute.Moe.Services.Randomness;

public static class IDiceRollerExtensions
{
    public static bool Flip(this IDiceRoller dice)
    {
        return dice.Roll(2) == 1;
    }

    public static ulong Roll(this IDiceRoller roller, uint dice, uint sides)
    {
        var total = 0ul;
        for (var i = 0u; i < dice; i++)
            total += roller.Roll(sides);
        return total;
    }

    public static ulong Roll(this IDiceRoller roller, uint dice, uint sides, uint explode)
    {
        var total = 0ul;
        for (var i = 0u; i < dice; i++)
            total += RollSingle();
        return total;

        ulong RollSingle()
        {
            var value = roller.Roll(1, sides);
            if (value >= explode)
                value += roller.Roll(1, sides);
            return value;
        }
    }
}

public interface IDiceRoller
{
    ulong Roll(ulong sides);
}