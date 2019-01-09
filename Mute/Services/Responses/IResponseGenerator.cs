using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Context;

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
        /// If possible create a conversation based on this message
        /// </summary>
        /// <param name="context"></param>
        /// <param name="containsMention"></param>
        /// <returns></returns>
        [NotNull, ItemCanBeNull] Task<IConversation> TryRespond([NotNull] MuteCommandContext context, bool containsMention);
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
        [NotNull, ItemCanBeNull] Task<string> Respond([NotNull] MuteCommandContext message, bool containsMention, CancellationToken ct);
    }
}
