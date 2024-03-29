﻿using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

public class WarnSlashComandMigrationAttribute(string command)
    : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command1, IServiceProvider services)
    {
        // Attach a "SlashCommandMigrationWarningSent" object, to prevent this from spamming multiple warnings for one message
        if (context is MuteCommandContext mute)
        {
            if (mute.TryGet<SlashCommandMigrationWarningSent>(out _))
                return PreconditionResult.FromSuccess();
            mute.GetOrAdd(() => new SlashCommandMigrationWarningSent());
        }

        await context.Channel.SendMessageAsync($"You should use the new slash command `/{command}` instead!");

        return PreconditionResult.FromSuccess();
    }

    private class SlashCommandMigrationWarningSent;
}