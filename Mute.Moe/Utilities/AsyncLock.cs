using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Utilities;

/// <summary>
/// Provide a lock that can be used in an async context
/// </summary>
public class AsyncLock
{
    private readonly Lock _mutex = new();

    private readonly Queue<TaskCompletionSource<IDisposable>> _waiting = new();

    /// <summary>
    /// Indicates if this lock is currently held
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Acquire the lock. Task blocks until lock is acquired.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A disposable which releases the lock</returns>
    public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        lock (_mutex)
        {
            if (!IsLocked)
            {
                // If the lock is available, take it immediately.
                IsLocked = true;
                return Task.FromResult<IDisposable>(new LockLifetime(this));
            }

            var completion = new TaskCompletionSource<IDisposable>();
            cancellationToken.Register(completion.SetCanceled);
            _waiting.Enqueue(completion);

            return completion.Task;
        }
    }

    private void Release()
    {
        lock (_mutex)
        {
            IsLocked = false;
            while (_waiting.Count > 0)
            {
                var tcs = _waiting.Dequeue();
                if (tcs.Task.IsCanceled)
                    continue;

                IsLocked = true;
                tcs.SetResult(new LockLifetime(this));
                return;
            }
        }
    }

    private class LockLifetime
        : IDisposable
    {
        private readonly AsyncLock _parent;
        private bool _released;

        public LockLifetime(AsyncLock parent)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            if (_released)
                throw new InvalidOperationException("Lock is already released");

            _parent.Release();
            _released = true;
        }
    }
}