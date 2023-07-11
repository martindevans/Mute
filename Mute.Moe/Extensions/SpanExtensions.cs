namespace Mute.Moe.Extensions
{
    public static class SpanExtensions
    {
        public static bool StartsWith<T>(this ReadOnlyMemory<T> span, ReadOnlySpan<T> query)
        {
            return span.Span.StartsWith(query);
        }

        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> query)
        {
            if (span.Length < query.Length)
                return false;

            return span[..query.Length] == query;
        }
    }
}
