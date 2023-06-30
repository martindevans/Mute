using System.Threading.Tasks;
using Autofocus;
using MoreLinq;

namespace Mute.Moe.Services.ImageGen
{
    public class StableDiffusionBackendCache
    {
        private readonly IReadOnlyList<BackendStatus> _backends;

        public StableDiffusionBackendCache(Configuration config)
        {
            var urls = config.Automatic1111?.Urls ?? Array.Empty<string>();
            _backends = urls.Select(a => new BackendStatus(a)).ToArray();
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

        public async Task<IReadOnlyList<(string, uint, bool)>> GetBackends(bool check)
        {
            if (check)
                await Task.WhenAll(_backends.Select(a => a.BeginStatusCheck()));

            var results = new List<(string, uint, bool)>();
            foreach (var backend in _backends)
                results.Add((backend.Name, backend.UsedCount, backend.IsResponsive));
            return results;
        }

        private class BackendStatus
        {
            public bool IsResponsive { get; private set; }
            public DateTime LastCheck { get; private set; }
            public uint UsedCount { get; private set; }

            public string Name { get; private set; }

            private readonly StableDiffusion _backend;

            public BackendStatus(string url)
            {
                _backend = new StableDiffusion(url);

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
}
