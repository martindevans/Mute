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
    /// Buffer items from a source until a certain amount of time has passed, and then return that batch in one go.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="timeSpan"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<IList<TSource>> BufferByTime<TSource>(this IAsyncEnumerable<TSource> source, TimeSpan timeSpan)
    {
        var buffer = new List<TSource>();
        var lastFlush = DateTime.UtcNow;

        await foreach (var item in source)
        {
            buffer.Add(item);

            if (DateTime.UtcNow - lastFlush >= timeSpan)
            {
                yield return buffer;

                buffer = [ ];
                lastFlush = DateTime.UtcNow;
            }
        }

        if (buffer.Count > 0)
            yield return buffer;
    }

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
}