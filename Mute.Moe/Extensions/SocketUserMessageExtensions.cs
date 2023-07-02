using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Extensions
{
    public static class SocketUserMessageExtensions
    {
        /// <summary>
        /// Get all image attachments from this message or the message it references
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IReadOnlyList<IAttachment> GetMessageImageAttachments(this IUserMessage message)
        {
            var attachments = message.Attachments.ToList<IAttachment>();
            attachments.AddRange(message.ReferencedMessage?.Attachments ?? Array.Empty<IAttachment>());

            var result = attachments
                .Where(a => a.ContentType.StartsWith("image/"))
                .ToList();

            return result;
        }

        /// <summary>
        /// Get all image attachments from this message or the message it references
        /// </summary>
        /// <param name="message"></param>
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
}
