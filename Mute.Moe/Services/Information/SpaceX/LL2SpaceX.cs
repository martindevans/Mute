using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.SpaceX;

public class LL2SpaceX
    : ISpacexInfo
{
    private readonly string BaseUrl = "https://ll.thespacedevs.com";
    private readonly HttpClient _http;

    public LL2SpaceX(IHttpClientFactory http)
    {
        _http = http.CreateClient();

        if (Debugger.IsAttached)
            BaseUrl = "https://lldev.thespacedevs.com";
    }

    public async Task<ILaunchInfo?> NextLaunch()
    {
        var launches = await Upcoming(1);
        return launches.Count > 0 ? launches[0] : null;
    }

    public async Task<IReadOnlyList<ILaunchInfo>> Upcoming(int limit)
    {
        var results = new List<ILaunchInfo>();

        var json = await _http.GetStringAsync($"{BaseUrl}/2.2.0/launch/upcoming?search=SpaceX");
        var model = JsonConvert.DeserializeObject<LaunchCollectionModel>(json);
        if (model == null)
            return results;

        var now = DateTime.UtcNow;
        await foreach (var item in model.Enumerate(_http))
        {
            if (!item.Upcoming)
                continue;

            if (item.DateUtc.HasValue && item.DateUtc.Value > now)
                results.Add(item);

            if (item.DateUtc > now && item.DatePrecision != DatePrecision.Unknown && item.DatePrecision <= DatePrecision.Day)
                results.Add(item);

            if (results.Count >= limit)
                break;
        }

        return results;
    }

    #region model
    private class LaunchCollectionModel
    {
        //[JsonProperty("count")] private int _count;
        [JsonProperty("next"), UsedImplicitly] private string? _next;
        //[JsonProperty("previous")] private string? _previous;
        [JsonProperty("results"), UsedImplicitly] private List<LaunchModel?>? _results;

        public async IAsyncEnumerable<LaunchModel> Enumerate(HttpClient http)
        {
            var page = this;

            while (page != null)
            {
                if (_results != null)
                    foreach (var item in _results.Where(item => item != null))
                        yield return item!;

                if (page._next != null)
                {
                    var json = await http.GetStringAsync(page._next);
                    page = JsonConvert.DeserializeObject<LaunchCollectionModel>(json);
                }
                else
                    page = null;
            }
        }
    }

    private class LaunchModel
        : ILaunchInfo
    {
        [JsonProperty("id"), UsedImplicitly] private string _id = null!;
        //[JsonProperty("url"), UsedImplicitly] private string _url = null!;
        [JsonProperty("name"), UsedImplicitly] private string? _name;
        [JsonProperty("image"), UsedImplicitly] private string? _image;
        [JsonProperty("pad"), UsedImplicitly] private LaunchPadInfo _pad = null!;
        [JsonProperty("rocket"), UsedImplicitly] private VehicleInfo _vehicle = null!;
        [JsonProperty("net"), UsedImplicitly] private DateTime? _net;
        [JsonProperty("net_precision"), UsedImplicitly] public NetPrecision? NetPrecision;
        [JsonProperty("mission"), UsedImplicitly] private MissionInfo? _mission;
        [JsonProperty("status"), UsedImplicitly] private StatusContainer _status = null!;

        public string ID => _id;
        public string Name => _mission?.Name ?? _name ?? "";
        public long MissionNumber => _mission?.ID ?? 0;

        public IEnumerable<string> Images
        {
            get
            {
                if (_image != null)
                    yield return _image;
            }
        }

        public bool Upcoming => _status.Status is LaunchStatus.TBD or LaunchStatus.GoForLaunch;
        public bool? Success => Upcoming ? null : _status.Status == LaunchStatus.Successful;

        public DateTime? DateUtc => _net;

        public DatePrecision DatePrecision
        {
            get
            {
                if (!Enum.TryParse<DatePrecision>(NetPrecision?.Name ?? "", out var result))
                    return DatePrecision.Unknown;
                return result;
            }
        }

        public string Description => _mission?.Description ?? "";

        public ILaunchPadInfo LaunchPad => _pad;
        public IVehicleInfo Vehicle => _vehicle;
    }

    [UsedImplicitly]
    private class NetPrecision
    {
        [JsonProperty("name"), UsedImplicitly] private string? _name;

        public string? Name => _name;
    }

    [UsedImplicitly]
    private class MissionInfo
    {
        [JsonProperty("id"), UsedImplicitly] private long _id;
        [JsonProperty("name"), UsedImplicitly] private string _name = null!;
        [JsonProperty("description"), UsedImplicitly] private string? _description;

        public long ID => _id;
        public string Name => _name;
        public string? Description => _description;
    }

    [UsedImplicitly]
    private class StatusContainer
    {
        [JsonProperty("id"), UsedImplicitly] private LaunchStatus _status;

        public LaunchStatus Status => _status;
    }

    private enum LaunchStatus
    {
        GoForLaunch = 1,
        TBD = 2,
        Successful = 3,
        Failed = 4,
    }

    private class LaunchPadInfo
        : ILaunchPadInfo
    {
        [JsonProperty("id"), UsedImplicitly] private long _id;
        [JsonProperty("name"), UsedImplicitly] private string? _name;
        [JsonProperty("latitude"), UsedImplicitly] private double _latitude;
        [JsonProperty("longitude"), UsedImplicitly] private double _longitude;

        public long Id => _id;
        public string Name => _name ?? "";
        public double Longitude => _longitude;
        public double Latitude => _latitude;
    }

    private class VehicleInfo
        : IVehicleInfo
    {
        [JsonProperty("id"), UsedImplicitly] private long _id;
        [JsonProperty("configuration"), UsedImplicitly] private VehicleConfiguration? _configuration;

        public long Id => _id;
        public string Name => _configuration?.Name ?? "";
    }

    private class VehicleConfiguration
    {
        [JsonProperty("name"), UsedImplicitly] private string? _name;
        [JsonProperty("family"), UsedImplicitly] private string? _family;
        [JsonProperty("url"), UsedImplicitly] private string? _url;

        public string Name => _name ?? "";
        public string Family => _family ?? "";
        public string Url => _url ?? "";
    }
    #endregion
}