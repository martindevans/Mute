using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Oddity.API.Models.DetailedCore;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Information.SpaceX
{
    public interface ISpacexInfo
    {
        [NotNull, ItemNotNull] Task<LaunchInfo> NextLaunch();

        [NotNull, ItemCanBeNull] Task<IReadOnlyList<LaunchInfo>> Launch(int id);

        [NotNull, ItemCanBeNull] Task<DetailedCoreInfo> Core(string id);

        [NotNull, ItemNotNull] Task<IReadOnlyList<LaunchInfo>> Upcoming();

        [NotNull, ItemNotNull] Task<IRoadsterInfo> Roadster();
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
