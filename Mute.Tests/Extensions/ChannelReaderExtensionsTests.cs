using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class ChannelReaderExtensionsTests
    {
        [TestMethod]
        public async Task WaitToReadWithTimeout_DataAvailable_ReturnsReadyToRead()
        {
            var channel = Channel.CreateUnbounded<int>();
            await channel.Writer.WriteAsync(42);

            var result = await channel.Reader.WaitToReadWithTimeout<int>(TimeSpan.FromSeconds(5));

            Assert.AreEqual(ChannelReaderExtensions.WaitToReadResult.ReadyToRead, result);
        }

        [TestMethod]
        public async Task WaitToReadWithTimeout_ChannelCompleted_ReturnsEndOfStream()
        {
            var channel = Channel.CreateUnbounded<int>();
            channel.Writer.Complete();

            var result = await channel.Reader.WaitToReadWithTimeout<int>(TimeSpan.FromSeconds(5));

            Assert.AreEqual(ChannelReaderExtensions.WaitToReadResult.EndOfStream, result);
        }

        [TestMethod]
        public async Task WaitToReadWithTimeout_TimeoutElapses_ReturnsTimeout()
        {
            var channel = Channel.CreateUnbounded<int>();

            var result = await channel.Reader.WaitToReadWithTimeout<int>(TimeSpan.FromMilliseconds(50));

            Assert.AreEqual(ChannelReaderExtensions.WaitToReadResult.Timeout, result);
        }

        [TestMethod]
        public async Task WaitToReadWithTimeout_CancellationTokenCancelled_ThrowsOperationCanceledException()
        {
            var channel = Channel.CreateUnbounded<int>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => channel.Reader.WaitToReadWithTimeout<int>(TimeSpan.FromSeconds(5), cts.Token).AsTask()
            );
        }

        [TestMethod]
        public async Task WaitToReadWithTimeout_CancellationTokenCancelledDuringWait_ThrowsOperationCanceledException()
        {
            var channel = Channel.CreateUnbounded<int>();
            using var cts = new CancellationTokenSource();

            // Cancel the token while WaitToReadWithTimeout is already waiting
            var waitTask = channel.Reader.WaitToReadWithTimeout<int>(TimeSpan.FromSeconds(5), cts.Token).AsTask();
            await cts.CancelAsync();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => waitTask);
        }
    }
}
