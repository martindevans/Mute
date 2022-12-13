using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;


namespace Mute.Moe.Discord.Attributes;

public class RequireVoiceChannel
    : PreconditionAttribute 
{
    public override async Task<PreconditionResult> CheckPermissionsAsync( ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        return context.User is IVoiceState
             ? PreconditionResult.FromSuccess()
             : PreconditionResult.FromError("Not in a voice channel");
    }
}