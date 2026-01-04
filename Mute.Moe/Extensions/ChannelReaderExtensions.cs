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
    public static async ValueTask<bool> WaitToReadWithTimeout<T>(this ChannelReader<T> reader, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        return await reader
            .WaitToReadAsync(cts.Token)
            .ConfigureAwait(false);
    }
}