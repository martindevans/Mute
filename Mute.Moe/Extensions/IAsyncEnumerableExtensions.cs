namespace Mute.Moe.Extensions;

public static class IAsyncEnumerableExtensions
{
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
}