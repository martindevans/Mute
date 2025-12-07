namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    /// Pick a random element from an enumerable. Enumerates it to completion
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="random"></param>
    /// <returns></returns>
    public static T? Random<T>(this IEnumerable<T>? items, Random random)
    {
        if (items == null)
            return default;

        // Pick first element (probability 1)
        // Later, for kth element pick it with probability 1/k (i.e. replace the existing selection with kth element)

        var result = default(T);

        var i = 0;
        foreach (var item in items)
        {
            if (random.NextDouble() < 1.0 / (i + 1))
                result = item;

            i++;
        }

        return result;
    }
}