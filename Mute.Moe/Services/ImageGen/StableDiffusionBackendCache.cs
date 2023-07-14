using System.Threading.Tasks;
using Autofocus;
using MoreLinq;

namespace Mute.Moe.Services.ImageGen;

public class StableDiffusionBackendCache
{
    private readonly IReadOnlyList<BackendStatus> _backends;

    public StableDiffusionBackendCache(Configuration config)
    {
        var urls = config.Automatic1111?.Urls ?? Array.Empty<string>();

        var timeoutSlow = TimeSpan.FromSeconds(config.Automatic1111?.GenerationTimeOutSeconds ?? 120);
        var timeoutFast = TimeSpan.FromSeconds(config.Automatic1111?.FastTimeOutSeconds ?? 7);

        _backends = urls.Select(a => new BackendStatus(a, timeoutSlow, timeoutFast)).ToArray();
    }

    public async Task<StableDiffusion?> GetBackend()
    {
        var initialResponsive = _backends.Where(a => a.IsResponsive).ToList();
        var initialDead = _backends.Where(a => !a.IsResponsive).ToList();

        // Kick off tasks to check all non-responsive backends if enough time has elapsed
        var checks = new List<Task>();
        foreach (var item in initialDead)
        {
            if (initialResponsive.Count == 0 || DateTime.UtcNow - item.LastCheck > TimeSpan.FromMinutes(5))
                checks.Add(item.BeginStatusCheck());
        }

        // Pick a random responsive backend
        if (initialResponsive.Count > 0)
        {
            foreach (var item in initialResponsive.Shuffle())
            {
                await item.BeginStatusCheck();
                if (item.IsResponsive)
                    return item.Backend();
            }
        }

        // There were no responsive backends! Wait for checks to finish
        while (checks.Count > 0)
        {
            await Task.WhenAny(checks);
            checks.RemoveAll(a => a.IsCompleted);

            var responsive = _backends.Where(a => a.IsResponsive).Shuffle().FirstOrDefault();
            if (responsive != null)
                return responsive.Backend();
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
    {
        public bool IsResponsive { get; private set; }
        public DateTime LastCheck { get; private set; }
        public uint UsedCount { get; private set; }

        public string Name { get; }

        private readonly StableDiffusion _backend;

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

        public StableDiffusion Backend()
        {
            UsedCount++;
            return _backend;
        }
    }
}