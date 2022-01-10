using System;
using System.Collections.Generic;

namespace Mute.Moe.Extensions
{
    public static class IEnumerableExtensions
    {
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

        public static T RandomNotNull<T>(this IEnumerable<T> items, Random random)
        {
            // Pick first element (probability 1)
            // Later, for kth element pick it with probability 1/k (i.e. replace the existing selection with kth element)

            var isNull = true;
            var result = default(T);

            var i = 0;
            foreach (var item in items)
            {
                if (random.NextDouble() < 1.0 / (i + 1))
                {
                    result = item;
                    isNull = false;
                }

                i++;
            }

            if (isNull)
                throw new InvalidOperationException("items must contains at least one item");

            return result!;
        }
    }
}
