using Discord.Commands;
using Discord.WebSocket;
using Mute.Moe.Services.Telemetry;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context;

/// <summary>
/// Context for execution of a command within *Mute
/// </summary>
public sealed class MuteCommandContext
    : SocketCommandContext, IAsyncDisposable
{
    /// <summary>
    /// The service provider
    /// </summary>
    public IServiceProvider Services { get; }

    private readonly ConcurrentDictionary<Type, Task> _resources = [ ];
    private readonly List<Func<MuteCommandContext, Task>> _completions = [ ];

    /// <summary>
    /// Instrumentation activity
    /// </summary>
    public Activity? Activity { get; set; }

    /// <summary>
    /// Get the context ID to use for memories
    /// </summary>
    public ulong AgentMemoryContextId => Channel.GetAgentMemoryContextId();

    /// <summary>
    /// Context for execution of a command within *Mute
    /// </summary>
    /// <param name="client"></param>
    /// <param name="msg"></param>
    /// <param name="services"></param>
    /// <param name="instrumentation"></param>
    public MuteCommandContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider services, Instrumentation instrumentation)
        : base(client, msg)
    {
        Services = services;
        Activity = instrumentation.ActivitySource.StartActivity();
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
        Activity?.AddEvent(new ActivityEvent("MuteCommandContext.TryGet", tags: new ActivityTagsCollection
        {
            { "Type", typeof(T).Name },
        }));

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
        Activity?.AddEvent(new ActivityEvent("MuteCommandContext.GetOrAdd", tags: new ActivityTagsCollection
        {
            { "Type", typeof(T).Name },
        }));

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
        Activity?.AddEvent(new ActivityEvent("MuteCommandContext.GetOrAddAsync", tags: new ActivityTagsCollection
        {
            { "Type", typeof(T).Name },
        }));

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
        try
        {
            foreach (var completion in _completions)
                await completion(this);
        }
        finally
        {
            Activity?.Dispose();
        }
    }
    #endregion
}