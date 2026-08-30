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

        return new EndExecute(context.Message, _emote, context.Client.CurrentUser, DateTime.UtcNow);
    }

    private class EndExecute(IMessage message, IEmote emote, IUser self, DateTime start)
        : IEndExecute
    {
        async Task IEndExecute.EndExecute()
        {
            // How much time elapsed time the start
            var elapsed = DateTime.UtcNow - start;

            // Ensure that we don't try to remove the reaction too quickly. This makes sure we don't
            // exhaust rate limits for very fast responses
            var minDelay = TimeSpan.FromMilliseconds(150);
            if (elapsed < minDelay)
                await Task.Delay(minDelay - elapsed);

            await message.RemoveReactionAsync(emote, self);
        }
    }
}