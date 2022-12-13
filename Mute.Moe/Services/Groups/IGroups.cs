using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;


namespace Mute.Moe.Services.Groups;

public interface IGroups
{
    Task<bool> IsUnlocked(IRole role);

    IAsyncEnumerable<IRole> GetUnlocked(IGuild guild);

    Task Unlock( IRole role);

    Task Lock( IRole role);
}