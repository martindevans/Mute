using System;
using System.Collections.Generic;


namespace Mute.Moe.Extensions
{
    public static class IReadonlyListExtensions
    {
        public static T Random<T>( this IReadOnlyList<T> items,  Random rng)
        {
            var index = rng.Next(items.Count);
            return items[index];
        }
    }
}
