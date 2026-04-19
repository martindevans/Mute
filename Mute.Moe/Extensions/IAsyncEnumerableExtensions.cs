using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IAsyncEnumerable{TSource}"/>
/// </summary>
public static class IAsyncEnumerableExtensions
{
    /// <summary>
    /// Take the N largest items by key
    /// </summary>
    /// <param name="source"></param>
    /// <param name="k"></param>
    /// <param name="keySelector"></param>
    /// <param name="comparer"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> MaxNByKey<T, TKey>(
        this IAsyncEnumerable<T> source,
        int k,
        Func<T, TKey> keySelector,
        IComparer<TKey>? comparer = null,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        if (k <= 0)
            yield break;

        // Keep the best items in a heap
        var pq = new PriorityQueue<T, TKey>(comparer ?? Comparer<TKey>.Default);
        await foreach (var item in source.WithCancellation(ct))
        {
            var key = keySelector(item);
            pq.Enqueue(item, key);

            if (pq.Count > k)
                pq.Dequeue();
        }

        // Yield the items
        while (pq.Count > 0)
            yield return pq.Dequeue();
    }

    /// <summary>
    /// Projects each element of an async-enumerable sequence into consecutive non-overlapping buffers which are produced based on element count information.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence, and in the lists in the result sequence.</typeparam>
    /// <param name="source">Source sequence to produce buffers over.</param>
    /// <param name="count">Length of each buffer.</param>
    /// <returns>An async-enumerable sequence of buffers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than or equal to zero.</exception>
    public static IAsyncEnumerable<IList<TSource>> Buffer<TSource>(this IAsyncEnumerable<TSource> source, int count)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        return Core(source, count);

        static async IAsyncEnumerable<IList<TSource>> Core(IAsyncEnumerable<TSource> source, int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = new List<TSource>(count);

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                buffer.Add(item);

                if (buffer.Count == count)
                {
                    yield return buffer;

                    buffer = new List<TSource>(count);
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer;
            }
        }
    }

    /// <summary>
    /// Convert a task enumerable into an iasyncenumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IEnumerable<T>> task)
    {
        var enumerable = await task;

        foreach (var item in enumerable)
            yield return item;
    }
}