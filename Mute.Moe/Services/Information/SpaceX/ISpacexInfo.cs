using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oddity.API.Models.DetailedCore;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Information.SpaceX
{
    public interface ISpacexInfo
    {
        Task<LaunchInfo?> NextLaunch();

        Task<IReadOnlyList<LaunchInfo>?> Launch(int id);

        Task<DetailedCoreInfo?> Core(string id);

        Task<IReadOnlyList<LaunchInfo>> Upcoming();

        Task<IRoadsterInfo> Roadster();
    }

    public interface IRoadsterInfo
    {
        float SpeedKph { get; }
        float EarthDistanceKilometers { get; }
        float MarsDistanceKilometers { get; }
        TimeSpan Period { get; }

        uint NoradId { get; }
        string Name { get; }

        DateTime LaunchTime { get; }

        string WikipediaUrl { get; }

        string OrbitType { get; }
    }
}
