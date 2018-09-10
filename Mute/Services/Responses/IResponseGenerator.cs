using System.Threading;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;

namespace Mute.Services.Responses
{
    public interface IResponse
    {
        /// <summary>
        /// Chance that this response will happen when valid
        /// </summary>
        double BaseChance { get; }

        /// <summary>
        /// Chance that this response will happen when mentioned directly
        /// </summary>
        double MentionedChance { get; }

        /// <summary>
        /// Quickly check if this response generator may want to respond to the given message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="containsMention"></param>
        /// <returns></returns>
        [ItemCanBeNull] Task<IConversation> TryRespond([NotNull] IMessage message, bool containsMention);
    }

    public interface IConversation
    {
        /// <summary>
        /// Get a value indicating if this conversation is finished
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Generate a response for the given message
        /// </summary>
        /// <returns></returns>
        [ItemCanBeNull] Task<string> Respond([NotNull] IMessage message, bool containsMention, CancellationToken ct);
    }
}
