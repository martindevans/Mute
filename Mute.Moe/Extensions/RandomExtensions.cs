namespace Mute.Moe.Extensions;

/// <summary>
/// 
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to <paramref name="min"/>,
    /// and less than <paramref name="max"/>.
    /// </summary>
    public static float NextSingle(this Random rng, float min, float max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(min), "Min must be less than or equal to Max.");

        return min + rng.NextSingle() * (max - min);
    }
}