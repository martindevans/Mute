using System;
using System.Collections.Concurrent;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Context
{
    public class MuteCommandContext
        : SocketCommandContext
    {
        public IServiceProvider Services { get; }

        private readonly ConcurrentDictionary<Type, object> _resources = new ConcurrentDictionary<Type, object>();

        public MuteCommandContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider services)
            : base(client, msg)
        {
            Services = services;
        }

        public bool TryGet<T>(out T value)
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

        [NotNull] public Task<T> GetOrAdd<T>(Func<Task<T>> create)
            where T : class
        {
            return Task.FromResult((T)_resources.GetOrAdd(typeof(T), _ => Task.Run(async () => await create()).Result));
        }

        public T GetOrAdd<T>(Func<T> create)
            where T : class
        {
            return (T)_resources.GetOrAdd(typeof(T), _ => create());
        }

        public TR GetOrAdd<TR, TV>(TV value, Func<TV, TR> create)
            where TR : class
        {
            return (TR)_resources.GetOrAdd(typeof(TR), (_, v) => create(v), value);
        }
    }
}
