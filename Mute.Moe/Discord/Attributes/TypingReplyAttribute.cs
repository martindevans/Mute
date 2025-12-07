using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Displays the `bot is typing...` notification in Discord while command is executing
/// </summary>
public class TypingReplyAttribute
    : BaseExecuteContextAttribute
{
    /// <inheritdoc />
    protected internal override IEndExecute StartExecute(MuteCommandContext context)
    {
        return new DisposableEnd(context.Channel.EnterTypingState());
    }
}