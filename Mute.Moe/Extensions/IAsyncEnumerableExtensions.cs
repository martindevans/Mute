using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions
{
    public static class IAsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> Delay<T>(this IAsyncEnumerable<T> items, int delay)
        {
            await foreach (var item in items)
            {
                await Task.Delay(delay);
                yield return item;
            }
        }
    }
}
