using System.Globalization;
using System.Text.RegularExpressions;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="string"/>
/// </summary>
public static class StringExtensions
{
    private static readonly Dictionary<string, string> CurrencyNameToSymbol;
    private static readonly Dictionary<string, string> CurrencySymbolToName;

    static StringExtensions()
    {
        CurrencyNameToSymbol = (from culture in CultureInfo.GetCultures(CultureTypes.AllCultures)
            where !culture.IsNeutralCulture
            let ri = RegionInfo(culture.LCID)
            where ri != null
            group ri by ri.ISOCurrencySymbol.ToLowerInvariant()
            into iso
            select (iso.Key, iso.First().CurrencySymbol)).ToDictionary(x => x.Key, x => x.CurrencySymbol);

        CurrencySymbolToName = new Dictionary<string, string>();
        foreach (var (key, value) in CurrencyNameToSymbol)
            CurrencySymbolToName[value] = key;
    }

    private static RegionInfo? RegionInfo(int lcid)
    {
        try
        {
            return new RegionInfo(lcid);
        }
        catch
        {
            return null;
        }
    }

    public static string TryGetCurrencySymbol(this string isoCurrencySymbol)
    {
        return CurrencyNameToSymbol.GetValueOrDefault(isoCurrencySymbol.ToLowerInvariant(), defaultValue: isoCurrencySymbol);
    }

    public static string? TryGetCurrencyIsoName(this string symbol)
    {
        return CurrencySymbolToName.GetValueOrDefault(symbol);
    }

    /// <summary>
    /// Distance between 2 strings
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static uint Levenshtein(this string? a, string? b)
    {
        if (a == null)
        {
            if (b == null)
                return 0;
            return (uint)b.Length;
        }

        if (b == null)
            return (uint)a.Length;

        var aLength = (uint)a.Length;
        var bLength = (uint)b.Length;
        var matrix = new int[aLength + 1, bLength + 1];

        for (var i = 0; i <= aLength;)
            matrix[i, 0] = i++;
        for (var j = 0; j <= bLength;)
            matrix[0, j] = j++;

        for (var i = 1; i <= aLength; i++)
        {
            for (var j = 1; j <= bLength; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return (uint)matrix[aLength, bLength];
    }

    /// <summary>
    /// Find all mentions of users, returns the IDs
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static IEnumerable<ulong> FindUserMentions(this string str)
    {
        return FindMentions(str, '@');
    }

    /// <summary>
    /// Find all mentions of channels, returns the IDs
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static IEnumerable<ulong> FindChannelMentions(this string str)
    {
        return FindMentions(str, '#');
    }

    /// <summary>
    /// Find mentions, returns them
    /// </summary>
    /// <param name="str"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    private static IEnumerable<ulong> FindMentions(this string str, char prefix)
    {
        var r = new Regex($@"\<{prefix}(!?)(?<id>[0-9]+)\>");

        var matches = r.Matches(str);

        return matches.SelectMany(m => m.Groups["id"].Captures.Select(c => c.Value)).Select(ulong.Parse);
    }

    /// <summary>
    /// string.split, but returning an enumerable of <see cref="ReadOnlyMemory{T}"/>
    /// </summary>
    /// <param name="str"></param>
    /// <param name="separator"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IEnumerable<ReadOnlyMemory<char>> SplitSpan(this string str, char separator, StringSplitOptions options = StringSplitOptions.None)
    {
        var start = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] == separator)
            {
                var length = i - start;
                if (!options.HasFlag(StringSplitOptions.RemoveEmptyEntries) || length > 0)
                    yield return str.AsMemory(start, i - start);
                start = i + 1;
            }
        }

        if (start != str.Length)
            yield return str.AsMemory(start, str.Length - start);
    }

    /// <summary>
    /// Limit a string to a given max length
    /// </summary>
    /// <param name="str"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string LimitLength(this string str, int maxLength)
    {
        if (str.Length < maxLength)
            return str;

        if (maxLength < 2)
            return str[..maxLength];

        return str[..(maxLength - 1)] + '…';
    }
}