using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class IAsyncEnumerableExtensions
    {
        public static async Task EnumerateAsync<T>([NotNull] this IAsyncEnumerable<T> enumerable, Func<T, Task> body)
        {
            using (var enumerator = enumerable.GetEnumerator())
                while (await enumerator.MoveNext())
                    await body(enumerator.Current);
        }
    }
}
