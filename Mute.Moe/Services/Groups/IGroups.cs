using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Groups
{
    public interface IGroups
    {
        Task<bool> IsUnlocked(IRole role);

        IAsyncEnumerable<IRole> GetUnlocked(IGuild guild);

        Task Unlock([NotNull] IRole role);

        Task Lock([NotNull] IRole role);
    }
}
