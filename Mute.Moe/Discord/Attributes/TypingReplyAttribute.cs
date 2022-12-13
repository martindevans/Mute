using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

public class TypingReplyAttribute
    : BaseExecuteContextAttribute
{
    protected internal override IEndExecute StartExecute(MuteCommandContext context)
    {
        return new DisposableEnd(context.Channel.EnterTypingState());
    }
}