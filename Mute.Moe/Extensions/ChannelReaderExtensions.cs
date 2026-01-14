using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="System.Threading.Channels.Channel"/>
/// </summary>
public static class ChannelReaderExtensions
{
    /// <summary>
    /// Wait until you can read from this channel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <param name="timeout"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="TaskCanceledException">Thrown if the cancellationToken is cancelled</exception>
    public static async ValueTask<WaitToReadResult> WaitToReadWithTimeout<T>(this ChannelReader<T> reader, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            var result = await reader
                .WaitToReadAsync(cts.Token)
                .ConfigureAwait(false);

            return result
                 ? WaitToReadResult.ReadyToRead
                 : WaitToReadResult.EndOfStream;
        }
        catch (TaskCanceledException)
        {
            // There are 2 ways we can get here. Either the timeout occured, or the root cancellation token
            // was cancelled. We want to handle them differently.

            // If the root token is cancelled, propagate that
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            return WaitToReadResult.Timeout;
        }
    }

    /// <summary>
    /// Results from <see cref="WaitToReadWithTimeout"/>
    /// </summary>
    public enum WaitToReadResult
    {
        /// <summary>
        /// Stream is ready to read
        /// </summary>
        ReadyToRead,

        /// <summary>
        /// End of stream, no more messages will ever arrive
        /// </summary>
        EndOfStream,

        /// <summary>
        /// Specified timeout elapsed
        /// </summary>
        Timeout
    }
}