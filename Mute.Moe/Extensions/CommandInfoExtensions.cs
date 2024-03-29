﻿using Discord.Commands;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

public static class CommandInfoExtensions
{
    public static async Task<bool> CheckCommandPreconditions(this CommandInfo command, ICommandContext context, IServiceProvider services)
    {
        var conditions = command.Preconditions.Concat(command.Module.Preconditions);

        foreach (var precondition in conditions)
            if (!(await precondition.CheckPermissionsAsync(context, command, services)).IsSuccess)
                return false;

        return true;
    }
}