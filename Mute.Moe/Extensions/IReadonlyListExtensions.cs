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

    /// <summary>
    /// Given a set of lines, find how much whitespace is at the start of all of them.
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static int MinimumCommonWhitespacePrefix(this IEnumerable<string> lines)
    {
        var min = int.MaxValue;

        foreach (var line in lines)
        {
            // Ignore blank lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Count whitespace at start
            var trimmed = line.AsSpan().TrimStart(' ');
            var count = line.Length - trimmed.Length;

            // Keep min prefix
            min = Math.Min(count, min);
        }

        return min == int.MaxValue ? 0 : min;
    }
}