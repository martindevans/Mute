using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Attributes
{
    public class RequireVoiceChannel
        : PreconditionAttribute 
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync([NotNull] ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is IVoiceState)
                return PreconditionResult.FromSuccess();
            else
                return PreconditionResult.FromError("Not in a voice channel");
        }
    }
}