using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mute.Moe.Discord.Attributes
{
    public class WarnDebuggerAttribute
        : PreconditionAttribute 
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Debugger.IsAttached)
                await context.Channel.SendMessageAsync("**Warning - Debugger is attached. This is likely not the main version of \\*Mute!**");

            return PreconditionResult.FromSuccess();
        }
    }
}
