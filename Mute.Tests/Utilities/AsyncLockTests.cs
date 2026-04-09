using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Utilities;

namespace Mute.Tests.Utilities;

[TestClass]
public class AsyncLockTests
{
    [TestMethod]
    public async Task AcquireLock_WhenFree_ReturnsImmediately()
    {
        var asyncLock = new AsyncLock();

        var lockTask = asyncLock.LockAsync();

        Assert.IsTrue(lockTask.IsCompleted);

        var handle = await lockTask;
        Assert.IsTrue(asyncLock.IsLocked);
        handle.Dispose();
    }

    [TestMethod]
    public async Task IsLocked_IsTrueWhileHeld_FalseAfterDispose()
    {
        var asyncLock = new AsyncLock();

        Assert.IsFalse(asyncLock.IsLocked);

        var handle = await asyncLock.LockAsync();
        Assert.IsTrue(asyncLock.IsLocked);

        handle.Dispose();
        Assert.IsFalse(asyncLock.IsLocked);
    }

    [TestMethod]
    public async Task AcquireLock_WhenHeld_BlocksUntilReleased()
    {
        var asyncLock = new AsyncLock();

        var first = await asyncLock.LockAsync();
        Assert.IsTrue(asyncLock.IsLocked);

        var secondTask = asyncLock.LockAsync();
        Assert.IsFalse(secondTask.IsCompleted, "Second acquisition should be waiting while first is held");

        // Release the first lock — second should now complete
        first.Dispose();

        var second = await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(asyncLock.IsLocked);

        second.Dispose();
        Assert.IsFalse(asyncLock.IsLocked);
    }

    [TestMethod]
    public async Task MultipleWaiters_AreGrantedLockInFifoOrder()
    {
        var asyncLock = new AsyncLock();
        var order = new List<int>();

        var first = await asyncLock.LockAsync();

        // Enqueue three waiters; each records its number then immediately releases
        var t1 = asyncLock.LockAsync().ContinueWith(t => { order.Add(1); t.Result.Dispose(); });
        var t2 = asyncLock.LockAsync().ContinueWith(t => { order.Add(2); t.Result.Dispose(); });
        var t3 = asyncLock.LockAsync().ContinueWith(t => { order.Add(3); t.Result.Dispose(); });

        // Give continuations a moment to be enqueued
        await Task.Delay(50);

        first.Dispose();
        await Task.WhenAll(t1, t2, t3).WaitAsync(TimeSpan.FromSeconds(5));

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
    }

    [TestMethod]
    public async Task CancelledWaiter_IsSkipped_NextWaiterGetsLock()
    {
        var asyncLock = new AsyncLock();

        var first = await asyncLock.LockAsync();

        using var cts = new CancellationTokenSource();
        var cancelledTask = asyncLock.LockAsync(cts.Token);

        // Enqueue a second waiter that is not cancelled
        // ReSharper disable once MethodSupportsCancellation
        var normalTask = asyncLock.LockAsync();

        // Cancel the first waiter before the lock is released
        // ReSharper disable once MethodHasAsyncOverload
        cts.Cancel();

        // Give the cancellation a moment to propagate
        // ReSharper disable once MethodSupportsCancellation
        await Task.Delay(50);

        first.Dispose();

        // The cancelled task should fault as cancelled
        await Assert.ThrowsAsync<TaskCanceledException>(() => cancelledTask);

        // The normal waiter should get the lock
        // ReSharper disable once MethodSupportsCancellation
        var handle = await normalTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(asyncLock.IsLocked);
        handle.Dispose();
    }

    [TestMethod]
    public async Task DoubleDispose_ThrowsInvalidOperationException()
    {
        var asyncLock = new AsyncLock();

        var handle = await asyncLock.LockAsync();
        handle.Dispose();

        Assert.Throws<InvalidOperationException>(handle.Dispose);
    }
}
