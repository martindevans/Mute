using System.Collections.Generic;
using System.Threading.Tasks;
using Oddity.Models.Cores;
using Oddity.Models.Launches;
using Oddity.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX
{
    public interface ISpacexInfo
    {
        Task<LaunchInfo?> NextLaunch();

        Task<LaunchInfo?> Launch(uint id);

        Task<CoreInfo?> Core(string id);

        Task<IReadOnlyList<LaunchInfo>> Upcoming();

        Task<RoadsterInfo> Roadster();
    }
}
