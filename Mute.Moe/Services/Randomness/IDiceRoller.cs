

namespace Mute.Moe.Services.Randomness;

/// <summary>
/// Extension methods for <see cref="IDiceRoller"/>
/// </summary>
public static class IDiceRollerExtensions
{
    /// <summary>
    /// Flip a coin (D2)
    /// </summary>
    /// <param name="dice"></param>
    /// <returns></returns>
    public static bool Flip(this IDiceRoller dice)
    {
        return dice.Roll(2) == 1;
    }
}

/// <summary>
/// Random number generator
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Roll an N sided dice
    /// </summary>
    /// <param name="sides"></param>
    /// <returns></returns>
    ulong Roll(ulong sides);
}