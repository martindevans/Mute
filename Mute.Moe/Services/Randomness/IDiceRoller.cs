using JetBrains.Annotations;

namespace Mute.Moe.Services.Randomness
{
    public static class IDiceRollerExtensions
    {
        public static bool Flip([NotNull] this IDiceRoller dice)
        {
            return dice.Roll(2) == 1;
        }
    }

    public interface IDiceRoller
    {
        ulong Roll(ulong sides);
    }
}
