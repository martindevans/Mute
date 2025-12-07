namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IReadOnlyList{T}"/>
/// </summary>
public static class IReadonlyListExtensions
{
    /// <summary>
    /// Select a random item from a list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="rng"></param>
    /// <returns></returns>
    public static T Random<T>(this IReadOnlyList<T> items, Random rng)
    {
        var index = rng.Next(items.Count);
        return items[index];
    }
}