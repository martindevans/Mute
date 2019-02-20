using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.AsyncEnumerable.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class IAsyncEnumerableExtensions
    {
        public static async Task EnumerateAsync<T>([NotNull] this IAsyncEnumerable<T> enumerable, Func<T, Task> body)
        {
            using (var enumerator = enumerable.GetEnumerator())
                while (await enumerator.MoveNext())
                    await body(enumerator.Current);
        }

        [NotNull] public static IOrderedAsyncEnumerable<T> AsOrderedEnumerable<T, TKey>([NotNull] this IAsyncEnumerable<T> enumerable, Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            return new OrderedEnumerable<T, TKey>(enumerable, keySelector, comparer);
        }

        private class OrderedEnumerable<T, TPrimaryKey>
            : IOrderedAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _enumerable;

            private readonly Func<T, TPrimaryKey> _keySelector;
            private readonly IComparer<TPrimaryKey> _comparer;

            public OrderedEnumerable(IAsyncEnumerable<T> enumerable, Func<T, TPrimaryKey> keySelector, IComparer<TPrimaryKey> comparer)
            {
                _enumerable = enumerable;
                _keySelector = keySelector;
                _comparer = comparer;
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                return _enumerable.GetEnumerator();
            }

            public IOrderedAsyncEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                var comp = new Comparer<TKey>(_keySelector, _comparer, keySelector, comparer);
                if (!descending)
                    return _enumerable.OrderBy(a => a, comp);
                else
                    return _enumerable.OrderByDescending(a => a, comp);
            }

            private class Comparer<TSecondaryKey>
                : IComparer<T>
            {
                private readonly Func<T, TPrimaryKey> _primaryKeySelector;
                private readonly IComparer<TPrimaryKey> _primaryComparer;

                private readonly Func<T, TSecondaryKey> _secondaryKeySelector;
                private readonly IComparer<TSecondaryKey> _secondaryComparer;

                public Comparer(Func<T, TPrimaryKey> primaryKeySelector, IComparer<TPrimaryKey> primaryComparer, Func<T, TSecondaryKey> secondaryKeySelector, IComparer<TSecondaryKey> secondaryComparer)
                {
                    _primaryKeySelector = primaryKeySelector;
                    _primaryComparer = primaryComparer;

                    _secondaryKeySelector = secondaryKeySelector;
                    _secondaryComparer = secondaryComparer;
                }

                public int Compare(T x, T y)
                {
                    //Compare by the base ordering, if that's not equal then return it
                    var pKeyX = _primaryKeySelector(x);
                    var pKeyY = _primaryKeySelector(y);
                    var pCompare = _primaryComparer.Compare(pKeyX, pKeyY);
                    if (pCompare != 0)
                        return pCompare;

                    //Now order by secondary
                    var sKeyX = _secondaryKeySelector(x);
                    var sKeyY = _secondaryKeySelector(y);
                    return _secondaryComparer.Compare(sKeyX, sKeyY);
                }
            }
        }

        public static async Task<T[]> ToArray<T>([NotNull] this Task<IAsyncEnumerable<T>> task)
        {
            return await (await task).ToArray();
        }

        public static async Task<T> FirstOrDefault<T>([NotNull] this Task<IAsyncEnumerable<T>> task)
        {
            return await (await task).FirstOrDefault();
        }

        public static async Task<IOrderedAsyncEnumerable<T>> OrderBy<T, TKey>([NotNull] this Task<IAsyncEnumerable<T>> task, Func<T, TKey> keySelector)
        {
            return (await task).OrderBy(keySelector);
        }
    }
}
