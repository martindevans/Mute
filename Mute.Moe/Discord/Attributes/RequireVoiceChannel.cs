using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Check that the user invoking this command is in a voice channel
/// </summary>
public class RequireVoiceChannel
    : PreconditionAttribute 
{
    /// <inheritdoc />
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        return context.User is IVoiceState
             ? PreconditionResult.FromSuccess()
             : PreconditionResult.FromError("Not in a voice channel");
    }
}