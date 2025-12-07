using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.RateLimit;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Send a warning that this text command should not be used, and the slash command should be preferred
/// </summary>
public class WarnSlashComandMigrationAttribute
    : PreconditionAttribute
{
    private readonly Guid _id;
    private readonly string _command;
    private readonly TimeSpan _rateLimitSeconds;

    /// <inheritdoc />
    public WarnSlashComandMigrationAttribute(string command, double rateLimitSeconds = 0, string? limitId = null)
    {
        _command = command;
        _rateLimitSeconds = TimeSpan.FromSeconds(rateLimitSeconds);
        _id = limitId == null ? Guid.NewGuid() : Guid.Parse(limitId);
    }

    /// <inheritdoc />
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command1, IServiceProvider services)
    {
        // Check if we should send this reminder again
        var rateLimit = services.GetService<IRateLimit>();
        if (rateLimit != null)
        {
            if (!await ShouldDisplayWarning(rateLimit))
                return PreconditionResult.FromSuccess();
            await rateLimit.Use(_id, 0);
        }

        // Attach a "SlashCommandMigrationWarningSent" object, to prevent this from spamming multiple warnings for one message
        if (context is MuteCommandContext mute)
        {
            if (mute.TryGet<SlashCommandMigrationWarningSent>(out _))
                return PreconditionResult.FromSuccess();
            mute.GetOrAdd(() => new SlashCommandMigrationWarningSent());
        }

        await context.Channel.SendMessageAsync($"You should use the new slash command `/{_command}` instead!");

        return PreconditionResult.FromSuccess();
    }

    private async Task<bool> ShouldDisplayWarning(IRateLimit limits)
    {
        var state = await limits.TryGetLastUsed(_id, 0);
        if (!state.HasValue)
            return true;

        var elapsed = DateTime.UtcNow - state.Value.LastUsed;
        if (elapsed > _rateLimitSeconds)
            return true;

        return false;
    }

    private class SlashCommandMigrationWarningSent;
}