using System.Globalization;
using System.Text.RegularExpressions;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="string"/>
/// </summary>
public static class StringExtensions
{
    private static readonly Dictionary<string, string> CurrencyNameToSymbol;

    static StringExtensions()
    {
        CurrencyNameToSymbol = (from culture in CultureInfo.GetCultures(CultureTypes.AllCultures)
            where !culture.IsNeutralCulture
            let ri = RegionInfo(culture.LCID)
            where ri != null
            group ri by ri.ISOCurrencySymbol.ToLowerInvariant()
            into iso
            select (iso.Key, iso.First().CurrencySymbol)).ToDictionary(x => x.Key, x => x.CurrencySymbol);
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

    /// <summary>
    /// Try to get the symbol for a currency. e.g. GBP => £
    /// </summary>
    /// <param name="isoCurrencySymbol"></param>
    /// <returns></returns>
    public static string TryGetCurrencySymbol(this string isoCurrencySymbol)
    {
        return CurrencyNameToSymbol.GetValueOrDefault(isoCurrencySymbol.ToLowerInvariant(), defaultValue: isoCurrencySymbol);
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

    /// <summary>
    /// Remove the "trim" span from the start of another span, if present. Ignoring case (OrdinalIgnoreCase)
    /// </summary>
    /// <param name="span"></param>
    /// <param name="trim"></param>
    /// <returns></returns>
    public static ReadOnlySpan<char> TrimStartCaseInsensitive(this ReadOnlySpan<char> span, ReadOnlySpan<char> trim)
    {
        while (span.StartsWith(trim, StringComparison.OrdinalIgnoreCase))
            span = span[trim.Length..];
        return span;
    }

    /// <summary>
    /// Replace a substring with a replacement in a string, with a count of how many replacements were done
    /// </summary>
    /// <param name="input"></param>
    /// <param name="substring"></param>
    /// <param name="replacement"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static string ReplaceWithCount(this string input, string substring, string replacement, out int count)
    {
        var accumulator = 0;

        var result = Regex.Replace(
            input,
            Regex.Escape(substring),
            _ =>
            {
                accumulator++;
                return replacement;
            });

        count = accumulator;
        return result;
    }

    /// <summary>
    /// Remove weird unicode characters that LLMs love to use
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string Deslop(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var span = input.ToArray().AsSpan();

        // Dashes
        span.Replace(span, '\u2012', '-'); // figure dash
        span.Replace(span, '\u2013', '-'); // en dash
        span.Replace(span, '\u2014', '-'); // em dash
        span.Replace(span, '\u2015', '-'); // horizontal bar
        span.Replace(span, '\u2212', '-'); // minus sign

        // Double quotes
        span.Replace(span, '\u201C', '"'); // left double quotation mark
        span.Replace(span, '\u201D', '"'); // right double quotation mark
        span.Replace(span, '\u201E', '"'); // double low-9 quotation mark
        span.Replace(span, '\u201F', '"'); // double high-reversed-9 quotation mark
        span.Replace(span, '\u00AB', '"'); // left-pointing double angle quotation mark
        span.Replace(span, '\u00BB', '"'); // right-pointing double angle quotation mark

        // Single quotes
        span.Replace(span, '\u2018', '\''); // left single quotation mark
        span.Replace(span, '\u2019', '\''); // right single quotation mark
        span.Replace(span, '\u201A', '\''); // single low-9 quotation mark
        span.Replace(span, '\u201B', '\''); // single high-reversed-9 quotation mark

        // Primes
        span.Replace(span, '\u2032', '\''); // prime
        span.Replace(span, '\u2033', '"');  // double prime

        // Bullets
        span.Replace(span, '\u2022', '-'); // bullet
        span.Replace(span, '\u2023', '-'); // triangular bullet
        span.Replace(span, '\u25E6', '-'); // white bullet
        span.Replace(span, '\u25AA', '-'); // black small square
        span.Replace(span, '\u25AB', '-'); // white small square

        // Spaces
        span.Replace(span, '\u00A0', ' '); // no-break space
        span.Replace(span, '\u2009', ' '); // thin space
        span.Replace(span, '\u200A', ' '); // hair space
        span.Replace(span, '\u202F', ' '); // narrow no-break space
        span.Replace(span, '\u205F', ' '); // medium mathematical space
        span.Replace(span, '\u3000', ' '); // ideographic space

        return new string(span)
            .Replace("\u2026", "...") // ellipsis
            .Replace("\u200B", "")    // zero-width space
            .Replace("\u200C", "")    // zero-width non-joiner
            .Replace("\u200D", "")    // zero-width joiner
            .Replace("\u2060", "")    // word joiner
            .Replace("\uFEFF", "")    // BOM
            .Replace("\u00AD", "");   // soft hyphen
    }
}