using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json.Serialization;
using Oddity;
using Oddity.API.Models.DetailedCore;
using Oddity.API.Models.Roadster;
using Oddity.API.Models.Launch;

namespace Mute.Moe.Services.Information.SpaceX
{
    public class OdditySpaceX
        : ISpacexInfo
    {
        public async Task<LaunchInfo?> NextLaunch()
        {
            try
            {
                using var o = new OddityCore();
                return await o.Launches.GetNext().ExecuteAsync();
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine($"`NextLaunch` failed: {e}");
                return null;
            }
        }

        public async Task<IReadOnlyList<LaunchInfo>?> Launch(int id)
        {
            using var o = new OddityCore();
            return (await o.Launches.GetAll().WithFlightNumber(id).ExecuteAsync());
        }

        public async Task<DetailedCoreInfo?> Core(string id)
        {
            using var o = new OddityCore();
            return (await o.DetailedCores.GetAbout(id).ExecuteAsync());
        }

        public async Task<IReadOnlyList<LaunchInfo>> Upcoming()
        {
            using var o = new OddityCore();
            o.OnDeserializationError += OddityOnOnDeserializationError;
            return (await o.Launches.GetUpcoming().ExecuteAsync());
        }

        public async Task<IRoadsterInfo> Roadster()
        {
            using var o = new OddityCore();
            return new OddityRoadsterInfo(await o.Roadster.Get().ExecuteAsync());
        }

        private static void OddityOnOnDeserializationError(object _,  ErrorEventArgs e)
        {
            Console.WriteLine("Oddity Serialization Error: " + e.ErrorContext.Path);
            e.ErrorContext.Handled = true;
        }

        private class OddityRoadsterInfo
            : IRoadsterInfo
        {
            private readonly RoadsterInfo _info;

            public float SpeedKph => _info.SpeedKph;
            public float EarthDistanceKilometers => _info.EarthDistanceKilometers;
            public float MarsDistanceKilometers => _info.MarsDistanceKilometers;
            public TimeSpan Period => TimeSpan.FromDays(_info.PeriodDays);
            public string OrbitType => _info.OrbitType;

            public uint NoradId => _info.NoradId;
            public string Name => _info.Name;

            public DateTime LaunchTime => _info.DateTimeUtc;

            public string WikipediaUrl => _info.Wikipedia;

            public OddityRoadsterInfo(RoadsterInfo info)
            {
                _info = info;
            }
        }
    }
}
