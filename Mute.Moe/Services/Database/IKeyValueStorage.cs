using System.Threading.Tasks;

namespace Mute.Moe.Services.Database;

/// <summary>
/// Simple persistent key/value storage. Keys are always <see cref="ulong"/>, values can be anything.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IKeyValueStorage<TValue>
    where TValue : class
{
    /// <summary>
    /// Get the value associated with the given key (if any)
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<TValue?> Get(ulong id);

    /// <summary>
    /// Store a value with the given key, replacing any existing value
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public Task Put(ulong id, TValue data);

    /// <summary>
    /// Delete the value associated with the given key (if any)
    /// </summary>
    /// <param name="id"></param>
    /// <returns>true, if there was an item to delete</returns>
    public Task<bool> Delete(ulong id);

    /// <summary>
    /// Count the total number of items
    /// </summary>
    /// <returns></returns>
    public Task<int> Count();

    /// <summary>
    /// Get a random item (if any)
    /// </summary>
    /// <returns></returns>
    public Task<TValue?> Random();
}