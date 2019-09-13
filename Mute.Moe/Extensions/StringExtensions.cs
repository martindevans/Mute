using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Mute.Moe.Extensions
{
    public static class StringExtensions
    {
        private static readonly IDictionary<string, string> CurrencyNameToSymbol;
        private static readonly IDictionary<string, string> CurrencySymbolToName;

        static StringExtensions()
        {
            CurrencyNameToSymbol = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => !c.IsNeutralCulture).Select(culture => RegionInfo(culture.LCID)).Where(ri => ri != null).GroupBy(ri => ri.ISOCurrencySymbol.ToLowerInvariant()).ToDictionary(x => x.Key, x => x.First().CurrencySymbol);

            CurrencySymbolToName = new Dictionary<string, string>();
            foreach (var kvp in CurrencyNameToSymbol)
                CurrencySymbolToName[kvp.Value] = kvp.Key;
        }

        [CanBeNull]
        private static RegionInfo RegionInfo(int lcid)
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

        public static string TryGetCurrencySymbol([NotNull] this string isoCurrencySymbol)
        {
            if (CurrencyNameToSymbol.TryGetValue(isoCurrencySymbol.ToLowerInvariant(), out var symbol))
                return symbol;
            return isoCurrencySymbol;
        }

        public static string TryGetCurrencyIsoName([NotNull] this string symbol)
        {
            if (CurrencySymbolToName.TryGetValue(symbol, out var result))
                return result;
            return null;
        }

        public static string SHA256(this string str)
        {
            var result = new StringBuilder();

            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(str));

                foreach (var b in bytes)
                    result.Append(b.ToString("x2"));
            }

            return result.ToString();
        }

        public static uint Levenshtein([CanBeNull] this string a, [CanBeNull] string b)
        {
            var an = string.IsNullOrEmpty(a);
            var bn = string.IsNullOrEmpty(b);

            if (an && bn)
                return 0;
            else if (an)
                return (uint)b.Length;
            else if (bn)
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
                    var cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost
                    );
                }
            }

            return (uint)matrix[aLength, bLength];
        }

        [NotNull] public static IEnumerable<ulong> FindUserMentions([NotNull] this string str)
        {
            return FindMentions(str, '@');
        }

        [NotNull] public static IEnumerable<ulong> FindChannelMentions([NotNull] this string str)
        {
            return FindMentions(str, '#');
        }

        [NotNull]
        private static IEnumerable<ulong> FindMentions([NotNull] this string str, char prefix)
        {
            var r = new Regex($"\\<{prefix}(!?)(?<id>[0-9]+)\\>");

            var matches = r.Matches(str);

            return matches.SelectMany(m => m.Groups["id"].Captures.Select(c => c.Value)).Select(ulong.Parse);
        }
    }
}
