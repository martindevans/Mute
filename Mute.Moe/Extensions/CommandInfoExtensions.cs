using Discord.Commands;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="CommandInfo"/>
/// </summary>
public static class CommandInfoExtensions
{
    /// <summary>
    /// Check if all command preconditions return true
    /// </summary>
    /// <param name="command"></param>
    /// <param name="context"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static async Task<bool> CheckCommandPreconditions(this CommandInfo command, ICommandContext context, IServiceProvider services)
    {
        var conditions = command.Preconditions.Concat(command.Module.Preconditions);

        foreach (var precondition in conditions)
            if (!(await precondition.CheckPermissionsAsync(context, command, services)).IsSuccess)
                return false;

        return true;
    }
}