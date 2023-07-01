using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Extensions
{
    public static class SocketUserMessageExtensions
    {
        public static async Task<IReadOnlyList<Stream>> GetMessageImages(this IUserMessage message, HttpClient http)
        {
            var attachments = message.Attachments.ToList<IAttachment>();
            attachments.AddRange(message.ReferencedMessage?.Attachments ?? Array.Empty<IAttachment>());

            var result = await attachments
                .ToAsyncEnumerable()
                .Where(a => a.ContentType.StartsWith("image/"))
                .SelectAwait(async a => await a.GetPngStream(http))
                .Where(a => a != null)
                .Select(a => a!)
                .ToListAsync();

            return result;
        }
    }
}
