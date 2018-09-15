using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class IEnumerableExtensions
    {
        [CanBeNull]
        public static T Random<T>([NotNull] this IEnumerable<T> items, [NotNull] Random random)
        {
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
}
