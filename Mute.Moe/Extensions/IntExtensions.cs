using System.Numerics;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="Int32"/>
/// </summary>
public static class IntExtensions
{
    /// <summary>
    /// Check if a number is a power of 2
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static bool IsPowerOfTwo(this int x)
    {
        return BitOperations.PopCount(unchecked((uint)x)) == 1;
    }
}