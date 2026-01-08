using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// Provide multiple API endpoints for fault tolerance and load balancing. When acquiring a backend a "slot" is booked for the
/// duration the backend is in use, so slot limits can be set per backend.
/// </summary>
public sealed class MultiEndpointProvider<TEndpoint>
{
    private readonly IReadOnlyList<Backend> _backends;
    private readonly HttpClient _healthCheckClient;

    /// <summary>
    /// Create a new provider
    /// </summary>
    /// <param name="http"></param>
    /// <param name="endpoints">Endpoints, in order of preference</param>
    public MultiEndpointProvider(IHttpClientFactory http, params EndpointConfig[] endpoints)
    {
        _backends = endpoints.Select(a => new Backend(a.Endpoint, a.Slots, a.HealthCheck)).ToArray();

        // Create a client with a short timeout, for health checks
        _healthCheckClient = http.CreateClient();
        _healthCheckClient.Timeout = TimeSpan.FromSeconds(0.5f);
    }

    /// <summary>
    /// Get an available endpoint which is healthy and take an available slot
    /// </summary>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<IScope?> GetEndpoint(CancellationToken cancellation)
    {
        // Start a health check on every backend
        var pending = (from backend in _backends
                      let task = _healthCheckClient.GetAsync(backend.HealthCheck, cancellation)
                      select (backend, task)).ToList();

        // Live backends
        var live = new List<Backend>();

        // Check all backends and add them to the live list. The first healthy backend in the
        // list that has capacity will be used.
        foreach (var (backend, task) in pending)
        {
            // Ignore items that fail the health check
            var response = await task;
            if (!response.IsSuccessStatusCode)
                continue;

            // Backend is alive!
            live.Add(backend);

            // Backend has capacity, use this one
            if (backend.AvailableSlots > 0)
                return await CreateScope(backend, TimeSpan.FromSeconds(0.1f), cancellation);
        }

        // Immediately fail if every backend failed the health check
        if (live.Count == 0)
            return null;

        // Cycle through backends trying to acquire a slot
        var remove = new List<Backend>();
        while (live.Count > 0 && !cancellation.IsCancellationRequested)
        {
            foreach (var backend in live)
            {
                // Do another health check
                if (!(await _healthCheckClient.GetAsync(backend.HealthCheck, cancellation)).IsSuccessStatusCode)
                {
                    remove.Add(backend);
                    continue;
                }

                // Try to acquire a scope for this backend
                if (backend.AvailableSlots > 0)
                {
                    var scope = await CreateScope(backend, TimeSpan.FromSeconds(0.1f), cancellation);
                    if (scope != null)
                        return scope;
                }
            }

            // Remove dead backends
            foreach (var item in remove)
                live.Remove(item);
            remove.Clear();

            // We checked every backend, and didn't acquire a slot from any of them! Wait a bit and try again
            await Task.Delay(TimeSpan.FromSeconds(0.1f), cancellation);
        }

        // No live backends available
        return default;

        static async ValueTask<Scope?> CreateScope(Backend backend, TimeSpan timeout, CancellationToken cancellation)
        {
            var acquired = await backend.Wait(timeout, cancellation);
            if (!acquired)
                return null;

            return new Scope(backend);
        }
    }

    /// <summary>
    /// An API backend
    /// </summary>
    private class Backend
    {
        /// <summary>
        /// Get the backend object
        /// </summary>
        public TEndpoint Endpoint { get; }

        /// <summary>
        /// Uri to ping to check backend health
        /// </summary>
        public Uri HealthCheck { get; }

        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// Number of slots available for use
        /// </summary>
        public int AvailableSlots => _semaphore.CurrentCount;

        /// <summary>
        /// Number of slots available for use
        /// </summary>
        public int TotalSlots { get; }

        /// <summary>
        /// Create a new backend
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="concurrentAccess"></param>
        /// <param name="healthCheck">URL to fetch for a health check</param>
        public Backend(TEndpoint endpoint, int concurrentAccess, Uri healthCheck)
        {
            Endpoint = endpoint;
            HealthCheck = healthCheck;
            _semaphore = new(concurrentAccess);
            TotalSlots = concurrentAccess;
        }

        /// <summary>
        /// Wait for this backend to become available
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _semaphore.WaitAsync(timeout, cancellationToken);
        }

        /// <summary>
        /// Release a slot to this backend
        /// </summary>
        public void Release()
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Configuration for an endpoint
    /// </summary>
    /// <param name="Endpoint"></param>
    /// <param name="Slots"></param>
    /// <param name="HealthCheck"></param>
    public sealed record EndpointConfig(TEndpoint Endpoint, int Slots, Uri HealthCheck);

    /// <summary>
    /// A scope of backend usage, while this is held a slot is consumed on the backend
    /// </summary>
    public interface IScope
        : IDisposable
    {
        /// <summary>
        /// Get the endpoint associated with this scope
        /// </summary>
        public TEndpoint Endpoint { get; }
    }

    /// <summary>
    /// A scope of backend usage, while this is held a slot is consumed on the backend
    /// </summary>
    private sealed class Scope
        : IScope
    {
        private readonly Backend _backend;
        private bool _released;

        /// <summary>
        /// Get the endpoint associated with this scope
        /// </summary>
        public TEndpoint Endpoint => _backend.Endpoint;

        /// <summary>
        /// Create a new scope. <b>Must acquire a semaphore slot **before** calling this!</b>
        /// </summary>
        /// <param name="backend"></param>
        public Scope(Backend backend)
        {
            _backend = backend;
            _released = false;
        }

        ~Scope()
        {
            Release();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        private void Release()
        {
            if (!_released)
                _backend.Release();
            _released = true;
        }
    }

    /// <summary>
    /// Get the status of all backends
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Status[]> GetStatus()
    {
        var pending = _backends.Select(async backend =>
        {
            var timer = Stopwatch.StartNew();
            var result = await _healthCheckClient.GetAsync(backend.HealthCheck);
            timer.Stop();
            return new Status(backend.Endpoint, backend.AvailableSlots, backend.TotalSlots, result.IsSuccessStatusCode, timer.Elapsed);
        }).ToList();

        return await Task.WhenAll(pending);
    }

    /// <summary>
    /// Status info for a backend
    /// </summary>
    /// <param name="Endpoint"></param>
    /// <param name="AvailableSlots"></param>
    /// <param name="MaxSlots"></param>
    /// <param name="Healthy"></param>
    /// <param name="Latency"></param>
    public record Status(TEndpoint Endpoint, int AvailableSlots, int MaxSlots, bool Healthy, TimeSpan Latency);
}