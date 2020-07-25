using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oddity;
using Oddity.Models.Cores;
using Oddity.Models.Launches;
using Oddity.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX
{
    public class OdditySpaceX
        : ISpacexInfo
    {
        private readonly OddityCore _oddity;

        public OdditySpaceX(OddityCore oddity)
        {
            _oddity = oddity;
        }

        public async Task<LaunchInfo?> NextLaunch()
        {
            try
            {
                return await _oddity.LaunchesEndpoint.GetNext().ExecuteAsync();
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine($"`NextLaunch` failed: {e}");
                return null;
            }
        }

        public async Task<IReadOnlyList<LaunchInfo>?> Launch(int id)
        {
            return await _oddity.LaunchesEndpoint.GetAll().ExecuteAsync();
        }

        public async Task<CoreInfo?> Core(string id)
        {
            return await _oddity.CoresEndpoint.Get(id).ExecuteAsync();
        }

        public async Task<IReadOnlyList<LaunchInfo>> Upcoming()
        {
            return await _oddity.LaunchesEndpoint.GetUpcoming().ExecuteAsync();
        }

        public async Task<RoadsterInfo> Roadster()
        {
            return await _oddity.RoadsterEndpoint.Get().ExecuteAsync();
        }
    }
}
