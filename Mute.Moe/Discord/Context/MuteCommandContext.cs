using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Mute.Moe.Discord.Context;

public class MuteCommandContext
    : SocketCommandContext, IAsyncDisposable
{
    public IServiceProvider Services { get; }

    private readonly ConcurrentDictionary<Type, object> _resources = new();

    private readonly List<Func<MuteCommandContext, Task>> _completions = new();

    public MuteCommandContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider services)
        : base(client, msg)
    {
        Services = services;
    }

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
        if (_resources.TryGetValue(typeof(T), out var obj))
        {
            value = (T)obj;
            return true;
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
    public Task<T> GetOrAdd<T>(Func<Task<T>> create)
        where T : class
    {
        return Task.FromResult((T)_resources.GetOrAdd(typeof(T), _ => Task.Run(async () => await create()).Result));
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
        return (T)_resources.GetOrAdd(typeof(T), _ => create());
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

    private class ActionCompletion
    {
        private readonly Action<MuteCommandContext> _action;

        public ActionCompletion(Action<MuteCommandContext> action)
        {
            _action = action;
        }

        public void Complete(MuteCommandContext context)
        {
            _action(context);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var completion in _completions)
            await completion(this);
    }
    #endregion
}