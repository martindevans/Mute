using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Oddity;
using Oddity.Models.Cores;
using Oddity.Models.Launches;
using Oddity.Models.Roadster;

namespace Mute.Moe.Services.Information.SpaceX;

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

    public async Task<LaunchInfo?> Launch(uint id)
    {
        var result = await _oddity.LaunchesEndpoint.Query()
            .WithFieldEqual(a => a.FlightNumber, id)
            .ExecuteAsync();

        return result.Data.Count != 1
             ? null
             : result.Data.Single();
    }

    public async Task<CoreInfo?> Core(string id)
    {
        var result = await _oddity.CoresEndpoint.Query()
            .WithFieldEqual(a => a.Serial, id)
            .WithLimit(1)
            .ExecuteAsync();

        await result.GoToFirstPage();
        return result.Data.Count != 1
             ? null
             : result.Data.Single();
    }

    public async Task<IReadOnlyList<LaunchInfo>> Upcoming(int limit)
    {
        var launches = await _oddity.LaunchesEndpoint.GetUpcoming().ExecuteAsync();
        while (launches.Count > limit)
            launches.RemoveAt(launches.Count - 1);

        return launches;
    }

    public async Task<RoadsterInfo> Roadster()
    {
        return await _oddity.RoadsterEndpoint.Get().ExecuteAsync();
    }
}