using System.Threading;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Services.Responses;

public class TerminalConversation
    : IConversation
{
    private readonly string? _response;
    private readonly IEmote[]? _reactions;

    public bool IsComplete { get; private set; }

    public TerminalConversation(string? response, params IEmote[]? reactions)
    {
        _response = response;
        _reactions = reactions;
    }

    public async Task<string?> Respond(MuteCommandContext context, bool containsMention, CancellationToken ct)
    {
        IsComplete = true;

        if (_reactions is {Length: > 0})
        {
            foreach (var reaction in _reactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                await context.Message.AddReactionAsync(reaction);
            }
        }

        return _response;
    }
}