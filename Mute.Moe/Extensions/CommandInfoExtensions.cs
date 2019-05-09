using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions
{
    public static class CommandInfoExtensions
    {
        public static async Task<bool> CheckCommandPreconditions([NotNull] this CommandInfo command, [NotNull] ICommandContext context, [NotNull] IServiceProvider services)
        {
            var conditions = command.Preconditions.Concat(command.Module.Preconditions);

            foreach (var precondition in conditions)
                if (!(await precondition.CheckPermissionsAsync(context, command, services)).IsSuccess)
                    return false;

            return true;
        }
    }
}
