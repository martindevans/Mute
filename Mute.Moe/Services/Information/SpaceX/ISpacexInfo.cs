using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.SpaceX;

public interface ISpacexInfo
{
    Task<ILaunchInfo?> NextLaunch();

    Task<IReadOnlyList<ILaunchInfo>> Upcoming(int limit);
}

public interface ILaunchInfo
{
    long MissionNumber { get; }
    string ID { get; }
    string Name { get; }
    string Description { get; }

    bool Upcoming { get; }
    bool? Success { get; }

    DateTime? DateUtc { get; }
    DatePrecision DatePrecision { get; }

    IEnumerable<string> Images { get; }
    ILaunchPadInfo LaunchPad { get; }
    IVehicleInfo Vehicle { get; }
}

public enum DatePrecision
{
    Unknown,

    Minute,
    Hour,
    Day,
    Month,
    Quarter,
    Half,
    Year
}

public interface ILaunchPadInfo
{
    long Id { get; }
    string Name { get; }

    double Longitude { get; }
    double Latitude { get; }
    string MapUrl => $"https://www.google.com/maps?q={Latitude},{Longitude}";
}

public interface IVehicleInfo
{
    public long Id { get; }
    public string Name { get; }
}