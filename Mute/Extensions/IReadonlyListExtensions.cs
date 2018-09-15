using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class IReadonlyListExtensions
    {
        public static T Random<T>([NotNull] this IReadOnlyList<T> items, [NotNull] Random rng)
        {
            var index = rng.Next(items.Count);
            return items[index];
        }
    }
}
