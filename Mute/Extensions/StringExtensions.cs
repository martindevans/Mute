using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class StringExtensions
    {
        private static readonly IDictionary<string, string> CurrencyNameToSymbol;
        private static readonly IDictionary<string, string> CurrencySymbolToName;

        static StringExtensions()
        {
            CurrencyNameToSymbol = CultureInfo.GetCultures(CultureTypes.AllCultures)
                             .Where(c => !c.IsNeutralCulture)
                             .Select(culture => RegionInfo(culture.LCID))
                             .Where(ri => ri != null)
                             .GroupBy(ri => ri.ISOCurrencySymbol.ToLowerInvariant())
                             .ToDictionary(x => x.Key, x => x.First().CurrencySymbol);

            CurrencySymbolToName = new Dictionary<string, string>();
            foreach (var kvp in CurrencyNameToSymbol)
                CurrencySymbolToName[kvp.Value] = kvp.Key;
        }

        [CanBeNull] private static RegionInfo RegionInfo(int lcid)
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
    }
}
