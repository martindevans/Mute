using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Utilities;

public class AsyncLock
{
    private readonly object _mutex = new();

    private bool _taken;
    private readonly Queue<TaskCompletionSource<IDisposable>> _waiting = new();

    public bool IsLocked => _taken;

    public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        lock (_mutex)
        {
            if (!_taken)
            {
                // If the lock is available, take it immediately.
                _taken = true;
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
            _taken = false;
            while (_waiting.Count > 0)
            {
                var tcs = _waiting.Dequeue();
                if (tcs.Task.IsCanceled)
                    continue;

                _taken = true;
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