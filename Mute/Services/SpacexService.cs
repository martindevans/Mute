using System.Collections.Generic;
using System.Threading.Tasks;
using Oddity;
using Oddity.API.Models.Launch;
using Oddity.API.Models.Roadster;

namespace Mute.Services
{
    public class SpacexService
    {
        public SpacexService()
        {
        }

        public async Task<LaunchInfo> NextLaunch()
        {
            using (var o = new OddityCore())
                return await o.Launches.GetNext().ExecuteAsync();
        }

        public async Task<IReadOnlyList<LaunchInfo>> Launch(int id)
        {
            using (var o = new OddityCore())
                return await o.Launches.GetAll().WithFlightNumber(id).ExecuteAsync();
        }

        public async Task<IReadOnlyList<LaunchInfo>> Upcoming()
        {
            using (var o = new OddityCore())
                return await o.Launches.GetUpcoming().ExecuteAsync();
        }

        public async Task<RoadsterInfo> Roadster()
        {
            using (var o = new OddityCore())
                return await o.Roadster.Get().ExecuteAsync();
        }
    }
}
