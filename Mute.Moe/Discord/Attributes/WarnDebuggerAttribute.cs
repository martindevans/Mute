using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

public class WarnDebuggerAttribute
    : PreconditionAttribute 
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        // Attach a "DebuggerWarningSent" object, to prevent this from spamming multiple warnings for one message
        if (context is MuteCommandContext mute)
        {
            if (mute.TryGet<DebuggerWarningSent>(out _))
                return PreconditionResult.FromSuccess();
            mute.GetOrAdd(() => new DebuggerWarningSent());
        }

        if (Debugger.IsAttached)
            await context.Channel.SendMessageAsync("**Warning - Debugger is attached. This is likely not the main version of \\*Mute!**");

        return PreconditionResult.FromSuccess();
    }

    private class DebuggerWarningSent
    {
    }
}