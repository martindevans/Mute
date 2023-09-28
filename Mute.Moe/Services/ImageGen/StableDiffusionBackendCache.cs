using System.Threading;
using System.Threading.Tasks;
using Autofocus;
using MoreLinq;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.ImageGen;

public class StableDiffusionBackendCache
{
    private readonly IReadOnlyList<BackendStatus> _backends;

    private readonly TimeSpan RecheckDeadBackendTime;

    public StableDiffusionBackendCache(Configuration config)
    {
        var urls = config.Automatic1111?.Urls ?? Array.Empty<string>();

        var timeoutSlow = TimeSpan.FromSeconds(config.Automatic1111?.GenerationTimeOutSeconds ?? 120);
        var timeoutFast = TimeSpan.FromSeconds(config.Automatic1111?.FastTimeOutSeconds ?? 7);
        RecheckDeadBackendTime = TimeSpan.FromSeconds(config.Automatic1111?.RecheckDeadBackendTime ?? 120);

        _backends = urls.Select(a => new BackendStatus(a, timeoutSlow, timeoutFast)).ToArray();
    }

    public async Task<IBackendAccessor?> GetBackend()
    {
        var probablyLive = _backends.Where(a => a.IsResponsive).ToList();
        var probablyDead = _backends.Where(a => !a.IsResponsive).ToList();

        // Kick off tasks to check all dead backends if enough time has elapsed or if there are no known live backends
        var deadChecks = (from item in probablyDead
                          where probablyLive.Count == 0 || DateTime.UtcNow - item.LastCheck > RecheckDeadBackendTime
                          select item.BeginStatusCheck()
                         ).ToList();

        // From all the backends that are probably live find the one with the shortest queue. If `GetQueueLength` does
        // not return a value (because the backend isn't really live) the backend will be marked as unresponsive.
        if (probablyLive.Count > 0)
        {
            var bestScore = float.MaxValue;
            BackendStatus? bestBackend = null;
            foreach (var item in probablyLive.Shuffle())
            {
                var eta = await item.QueueETA();
                if (eta < bestScore)
                {
                    bestScore = eta.Value;
                    bestBackend = item;

                    if (bestScore == 0)
                        return bestBackend;
                }
            }
            if (bestBackend != null)
                return bestBackend;
        }

        // There were no responsive backends! Wait for checks to finish
        while (deadChecks.Count > 0)
        {
            await Task.WhenAny(deadChecks);
            deadChecks.RemoveAll(a => a.IsCompleted);

            var responsive = _backends.Where(a => a.IsResponsive).Shuffle().FirstOrDefault();
            if (responsive != null)
                return responsive;
        }

        // No hope
        return null;
    }

    public async Task<IReadOnlyList<(string name, uint useCount, bool available)>> GetBackends(bool check)
    {
        if (check)
            await Task.WhenAll(_backends.Select(a => a.BeginStatusCheck()));

        return _backends
              .Select(backend => (backend.Name, backend.UsedCount, backend.IsResponsive))
              .ToList();
    }

    private class BackendStatus
        : IBackendAccessor
    {
        public bool IsResponsive { get; private set; }
        public DateTime LastCheck { get; private set; }
        public uint UsedCount { get; private set; }

        public string Name { get; }

        private readonly AsyncLock _lock = new();
        private readonly IStableDiffusion _backend;

        public BackendStatus(string url, TimeSpan slow, TimeSpan fast)
        {
            _backend = new StableDiffusion(url)
            {
                TimeoutSlow = slow,
                TimeoutFast = fast
            };

            LastCheck = DateTime.MinValue;
            IsResponsive = false;

            Name = new Uri(url).Host;
        }

        public Task BeginStatusCheck()
        {
            LastCheck = DateTime.UtcNow;

            return Task.Run(async () =>
            {
                try
                {
                    await _backend.Ping();
                    IsResponsive = true;
                }
                catch
                {
                    IsResponsive = false;
                }
            });
        }

        public async Task<float?> QueueETA()
        {
            var islocked = _lock.IsLocked;

            try
            {
                var progress = await _backend.Progress(true);
                var time = (float)progress.ETARelative.TotalSeconds;

                // Return a larger time is the lock is currently taken
                if (islocked)
                    return time * 4 + 8;
                return time;
            }
            catch (Exception)
            {
                LastCheck = DateTime.UtcNow;
                IsResponsive = false;
                return null;
            }
        }

        public async Task<BackendScope> Lock(CancellationToken token)
        {
            UsedCount++;

            return new BackendScope(
                _backend,
                await _lock.LockAsync(token)
            );
        }
    }

    public interface IBackendAccessor
    {
        public Task<BackendScope> Lock(CancellationToken token);
    }

    public readonly struct BackendScope
        : IDisposable
    {
        public IStableDiffusion Backend { get; }
        private readonly IDisposable _scope;

        public BackendScope(IStableDiffusion backend, IDisposable scope)
        {
            Backend = backend;
            _scope = scope;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}