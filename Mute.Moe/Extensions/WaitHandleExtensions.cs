using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions to <see cref="WaitHandle"/>
/// </summary>
public static class WaitHandleExtensions
{
    /// <summary>
    /// Async wait for the handle
    /// </summary>
    /// <param name="waitHandle"></param>
    /// <param name="timeout"></param>
    /// <returns>A task which completes when the handle is set. <see langword="true" /> if the handle was set, otherwise <see langword="false" /></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(waitHandle);

        // Use RunContinuationsAsynchronously to prevent the awaiting code from hijacking the ThreadPool callback thread.
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var rwh = ThreadPool.RegisterWaitForSingleObject(
            waitObject: waitHandle,
            callBack: static (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
            state: tcs,
            timeout: timeout,
            executeOnlyOnce: true
        );

        // - ExecuteSynchronously so unregistration happens immediately without queuing a new work item.
        // - TaskScheduler.Default to ensure we don't try to run this on a UI context if the method was called from one.
        tcs.Task.ContinueWith(
            _ => rwh.Unregister(null),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return tcs.Task;
    }
}