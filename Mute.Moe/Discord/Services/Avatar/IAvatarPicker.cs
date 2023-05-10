using System.Collections.Generic;
using System.Threading.Tasks;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Discord.Services.Avatar;

public interface IAvatarPicker
    : IHostedService
{
    Task<AvatarPickResult> PickAvatarNow();
}


public class AvatarPickResult
{
    public IReadOnlyList<string> Options { get; }
    public string? Choice { get; }

    public AvatarPickResult(IReadOnlyList<string> options, string? choice)
    {
        Options = options;
        Choice = choice;
    }
}