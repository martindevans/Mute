using System.Numerics;

namespace Mute.Moe.Extensions;

public static class IntExtensions
{
    public static bool IsPowerOfTwo(this int x)
    {
        return BitOperations.PopCount(unchecked((uint)x)) == 1;
    }
}