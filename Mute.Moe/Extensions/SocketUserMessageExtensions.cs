using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Extensions;

public static class SocketUserMessageExtensions
{
    /// <summary>
    /// Get all image attachments from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static IReadOnlyList<IAttachment> GetMessageImageAttachments(this IUserMessage message)
    {
        // Get all attachments for message
        var attachments = message.Attachments.ToList<IAttachment>();

        // Get all attachments from mentioned message (if any)
        attachments.AddRange(message.ReferencedMessage?.Attachments ?? []);

        // Remove all non image attachments
        attachments.RemoveAll(a => !a.ContentType.StartsWith("image/"));

        return attachments;
    }

    /// <summary>
    /// Get all image attachments from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <param name="http"></param>
    /// <returns></returns>
    public static async Task<IReadOnlyList<Stream>> GetMessageImages(this IUserMessage message, HttpClient http)
    {
        var result = await GetMessageImageAttachments(message)
                          .ToAsyncEnumerable()
                          .SelectAwait(async a => await a.GetPngStream(http))
                          .Where(a => a != null)
                          .Select(a => a!)
                          .ToListAsync();

        return result;
    }
}