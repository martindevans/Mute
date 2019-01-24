using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Serialization;
using Oddity;
using Oddity.API.Models.Launch;
using Oddity.API.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX
{
    public class OdditySpaceX
        : ISpacexInfo
    {
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
            {
                o.OnDeserializationError += OddityOnOnDeserializationError;
                return await o.Launches.GetUpcoming().ExecuteAsync();
            }
        }

        public async Task<RoadsterInfo> Roadster()
        {
            using (var o = new OddityCore())
                return await o.Roadster.Get().ExecuteAsync();
        }

        private void OddityOnOnDeserializationError(object _, [NotNull] ErrorEventArgs e)
        {
            Console.WriteLine("Oddity Serialization Error: " + e.ErrorContext.Path);
            e.ErrorContext.Handled = true;
        }
    }
}
