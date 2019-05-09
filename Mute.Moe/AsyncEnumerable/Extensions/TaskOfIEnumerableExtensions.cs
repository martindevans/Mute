using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.AsyncEnumerable.Extensions
{
    public static class TaskOfIEnumerableExtensions
    {
        [ItemNotNull] public static async Task<T[]> ToArray<T>([NotNull] this Task<IEnumerable<T>> enumerable)
        {
            return (await enumerable).ToArray();
        }
    }
}
