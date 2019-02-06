using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.AsyncEnumerable.Extensions
{
    public static class TaskOfIOrderedAsyncEnumerableExtensions
    {
        public static async Task<T[]> ToArray<T>([NotNull] this Task<IOrderedAsyncEnumerable<T>> task)
        {
            return await (await task).ToArray();
        }

        public static async Task<T> FirstOrDefault<T>([NotNull] this Task<IOrderedAsyncEnumerable<T>> task)
        {
            return await (await task).FirstOrDefault();
        }
    }
}
