using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// An emoji will be attached to the message which triggered this context for the duration of the response handler
/// </summary>
public class ThinkingReplyAttribute(string emote = EmojiLookup.Thinking)
    : BaseExecuteContextAttribute
{
    private readonly IEmote _emote = new Emoji(emote);

    /// <inheritdoc />
    protected internal override IEndExecute StartExecute(MuteCommandContext context)
    {
        context.Message.AddReactionAsync(_emote);

        return new EndExecute(context.Message, _emote, context.Client.CurrentUser);
    }

    private class EndExecute(IMessage message, IEmote emote, IUser self)
        : IEndExecute
    {
        Task IEndExecute.EndExecute()
        {
            return message.RemoveReactionAsync(emote, self);
        }
    }
}