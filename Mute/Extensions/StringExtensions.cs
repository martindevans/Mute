using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mute.Extensions
{
    public static class StringExtensions
    {
        private static IDictionary<string, string> map;

        static StringExtensions()
        {
            map = CultureInfo.GetCultures(CultureTypes.AllCultures)
                             .Where(c => !c.IsNeutralCulture)
                             .Select(culture => RegionInfo(culture.LCID))
                             .Where(ri => ri != null)
                             .GroupBy(ri => ri.ISOCurrencySymbol.ToLowerInvariant())
                             .ToDictionary(x => x.Key, x => x.First().CurrencySymbol);
        }

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

        public static string TryGetCurrencySymbol(this string isoCurrencySymbol)
        {
            if (map.TryGetValue(isoCurrencySymbol.ToLowerInvariant(), out var symbol))
                return symbol;
            return isoCurrencySymbol;
        }
    }
}
