using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Mute.Responses
{
    public interface IResponse
    {
        /// <summary>
        /// Get a value indicating if this response only runs when the bot is explicitly mentioned in the message
        /// </summary>
        bool RequiresMention { get; }

        /// <summary>
        /// Quickly check if this response generator may want to respond to the given message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="containsMention"></param>
        /// <returns></returns>
        bool MayRespond(IMessage message, bool containsMention);

        /// <summary>
        /// Generate a response for the given message
        /// </summary>
        /// <returns></returns>
        Task<string> Respond(IMessage message, bool containsMention, CancellationToken ct);
    }
}
