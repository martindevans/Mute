using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Mute.Moe.Discord.Context;

/// <summary>
/// Context for execution of a command within *Mute
/// </summary>
/// <param name="client"></param>
/// <param name="msg"></param>
/// <param name="services"></param>
public sealed class MuteCommandContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider services)
    : SocketCommandContext(client, msg), IAsyncDisposable
{
    /// <summary>
    /// The service provider
    /// </summary>
    public IServiceProvider Services { get; } = services;

    private readonly ConcurrentDictionary<Type, Task> _resources = [ ];
    private readonly List<Func<MuteCommandContext, Task>> _completions = [ ];

    /// <summary>
    /// Get the context ID to use for memories
    /// </summary>
    public ulong AgentMemoryContextId => Channel.GetAgentMemoryContextId();

    #region context
    /// <summary>
    /// Try to get context of type T that was previously attached. Null if nothing has attached this context
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet<T>(out T? value)
        where T : class
    {
        if (_resources.TryGetValue(typeof(T), out var task))
        {
            if (task.IsCompletedSuccessfully && task is Task<T> typed)
            {
                value = typed.Result;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get context of type T that was previously attached. Creates and attached T using the provided factory if nothing has attached this context
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="create"></param>
    /// <returns></returns>
    public T GetOrAdd<T>(Func<T> create)
        where T : class
    {
        // ReSharper disable once HeapView.CanAvoidClosure
        var task = (Task<T>)_resources.GetOrAdd(typeof(T), _ => Task.FromResult(create()));
        return task.Result;
    }

    /// <summary>
    /// Get context of type T that was previously attached. Creates and attached T using the provided factory if nothing has attached this context
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="create"></param>
    /// <returns></returns>
    public async Task<T> GetOrAddAsync<T>(Func<Task<T>> create)
        where T : class
    {
        // ReSharper disable once HeapView.CanAvoidClosure
        var task = (Task<T>)_resources.GetOrAdd(typeof(T), _ => create());
        return await task;
    }
    #endregion

    #region completions
    /// <summary>
    /// Register a "completion" which will receive a callback when this command context is being disposed
    /// </summary>
    /// <param name="completion"></param>
    public void RegisterCompletion(Func<MuteCommandContext, Task> completion)
    {
        _completions.Add(completion);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var completion in _completions)
            await completion(this);
    }
    #endregion
}