using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Mute.Moe.Discord.Services.ComponentActions;

/// <summary>
/// Provides a system for waiting on component responses (buttons, dropdowns etc) embedded in messages
/// </summary>
public class ComponentActionService
{
    private readonly ConcurrentDictionary<string, ButtonWaiter> _waiters = new();

    private readonly DiscordSocketClient _client;

    public ComponentActionService(DiscordSocketClient client)
    {
        _client = client;
        _client.ButtonExecuted += OnExecuted;
        _client.SelectMenuExecuted += OnExecuted;
    }

    private async Task OnExecuted(SocketMessageComponent args)
    {
        if (_waiters.Remove(args.Data.CustomId, out var waiter))
            waiter.Set(args);
    }

    /// <summary>
    /// Get a task that completes when the given socket message component ID is execited
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<SocketMessageComponent> GetWaiter(string id)
    {
        return _waiters.GetOrAdd(id, _ => new()).Task;
    }

    public void DestroyWaiter(string id)
    {
        _waiters.Remove(id, out _);
    }

    private class ButtonWaiter
    {
        private readonly TaskCompletionSource<SocketMessageComponent> _cts = new();
        public Task<SocketMessageComponent> Task => _cts.Task;

        public void Set(SocketMessageComponent args)
        {
            _cts.SetResult(args);
        }
    }
}