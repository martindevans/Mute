using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

public static class IAsyncEnumerableExtensions
{
    public static async Task<IReadOnlyList<T>> ToReadOnlyListAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var list = await enumerable.ToListAsync();
        return list;
    }
}