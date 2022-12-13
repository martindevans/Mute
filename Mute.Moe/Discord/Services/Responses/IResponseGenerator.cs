using System.Threading;
using System.Threading.Tasks;

using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// A possible response to a message which may start a conversation
/// </summary>
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
    Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention);
}

/// <summary>
/// A self contained discussion about something
/// </summary>
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
    Task<string?> Respond(MuteCommandContext message, bool containsMention, CancellationToken ct);
}