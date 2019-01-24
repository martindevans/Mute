using System.Collections.Generic;
using System.Threading.Tasks;
using Oddity.API.Models.Launch;
using Oddity.API.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX
{
    public interface ISpacexInfo
    {
        Task<LaunchInfo> NextLaunch();

        Task<IReadOnlyList<LaunchInfo>> Launch(int id);

        Task<IReadOnlyList<LaunchInfo>> Upcoming();

        Task<RoadsterInfo> Roadster();
    }
}
