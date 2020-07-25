using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Mute.Moe.Extensions
{
    public static class IEnumerableExtensions
    {
        [return: MaybeNull]
        public static T Random<T>(this IEnumerable<T> items,  Random random)
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

        public static T RandomNotNull<T>(this IEnumerable<T> items,  Random random)
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

        public static TV MinBy<TV, TK>( this IEnumerable<TV> items,  Func<TV, TK> keySelector)
            where TK : IComparable<TK>
        {
            return items
                .Select(a => new  { item = a, k = keySelector(a) })
                .Aggregate((a, b) => a.k.CompareTo(b.k) < 0 ? a : b)
                .item;
        }

        
        public static IEnumerable<TV> DistinctBy<TV, TK>( this IEnumerable<TV> items,  Func<TV, TK> keySelector)
            where TK : IEquatable<TK>
        {
            return items.Distinct(new DistinctByComparer<TV, TK>(keySelector));
        }

        private class DistinctByComparer<TV, TK>
            : IEqualityComparer<TV>
            where TK : IEquatable<TK>
        {
            private readonly Func<TV, TK> _keySelector;

            public DistinctByComparer(Func<TV, TK> keySelector)
            {
                _keySelector = keySelector;
            }

            public bool Equals([AllowNull] TV x, [AllowNull] TV y)
            {
                if (x is null && y is null)
                    return true;
                if (x is null || y is null)
                    return false;

                return _keySelector(x).Equals(_keySelector(y));
            }

            public int GetHashCode(TV obj)
            {
                return _keySelector(obj).GetHashCode();
            }
        }
    }
}
