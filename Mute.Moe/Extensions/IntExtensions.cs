namespace Mute.Moe.Extensions;

public static class IntExtensions
{
    public static bool IsPowerOfTwo(this int x)
    {
        return (x != 0) && ((x & (x - 1)) == 0);
    }
}