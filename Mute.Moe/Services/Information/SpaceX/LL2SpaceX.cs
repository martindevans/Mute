using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.SpaceX;

public class LL2SpaceX
    : ISpacexInfo
{
    private readonly string BaseUrl = "https://ll.thespacedevs.com";
    private readonly HttpClient _http;

    public LL2SpaceX(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient();

        if (Debugger.IsAttached)
            BaseUrl = "https://lldev.thespacedevs.com";
    }

    public async Task<ILaunchInfo?> NextLaunch()
    {
        var json = await _http.GetStringAsync($"{BaseUrl}/2.2.0/launch/upcoming?search=SpaceX");
        var model = JsonConvert.DeserializeObject<LaunchCollectionModel>(json);
        if (model == null)
            return null;

        var now = DateTime.UtcNow;
        await foreach (var item in model.Enumerate(_http).Take(25))
        {
            if (!item.Upcoming)
                continue;

            if (item.DateUtc.HasValue && item.DateUtc.Value > now)
                return item;

            if (!Enum.TryParse<DatePrecision>(item.NetPrecision?.Name ?? "", out var netPrecision))
                continue;

            if (item.DateUtc > now && netPrecision != DatePrecision.Unknown && netPrecision <= DatePrecision.Day)
                return item;
        }

        return null;
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

    private class LaunchCollectionModel
    {
        public int Count;

        public string? Next;
        public string? Previous;

        public List<LaunchModel?>? Results;

        public async IAsyncEnumerable<LaunchModel> Enumerate(HttpClient http)
        {
            var page = this;

            while (page != null)
            {
                if (Results != null)
                    foreach (var item in Results)
                        if (item != null)
                            yield return item;

                if (page.Next != null)
                {
                    var json = await http.GetStringAsync(page.Next);
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
        [JsonProperty("id")] private string _id = null!;
        [JsonProperty("name")] private string? _name;
        [JsonProperty("image")] private string? _image;
        [JsonProperty("pad")] private LaunchPad _pad = null!;
        [JsonProperty("rocket")] private Vehicle _vehicle = null!;
        [JsonProperty("net")] private DateTime? _net;
        [JsonProperty("net_precision")] public NetPrecision? NetPrecision = null!;

        public string ID => _id;
        public string Name => Mission?.Name ?? _name ?? "";

        public long MissionNumber => Mission?.ID ?? 0;

        public string Url = null!;

        public IEnumerable<string> Images
        {
            get
            {
                if (_image != null)
                    yield return _image;
            }
        }

        public MissionInfo Mission = null!;

        public StatusContainer Status = null!;


        public bool Upcoming => Status.Id is LaunchStatus.TBD or LaunchStatus.GoForLaunch;
        public bool? Success => Upcoming ? null : Status.Id == LaunchStatus.Successful;

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

        public string Description => Mission?.Description ?? "";

        public ILaunchPadInfo LaunchPad => _pad;
        public IVehicleInfo Vehicle => _vehicle;
    }

    private class NetPrecision
    {
        public string? Name;
    }

    private class MissionInfo
    {
        public long ID;
        public string Name = null!;
        public string? Description;
    }

    private class StatusContainer
    {
        public LaunchStatus Id;
    }

    private enum LaunchStatus
    {
        GoForLaunch = 1,
        TBD = 2,
        Successful = 3,
        Failed = 4,
    }

    private class LaunchPad
        : ILaunchPadInfo
    {
        [JsonProperty("id")] private long _id;
        [JsonProperty("name")] private string? _name;
        [JsonProperty("latitude")] private double _latitude;
        [JsonProperty("longitude")] private double _longitude;

        public long Id => _id;
        public string Name => _name ?? "";
        public double Longitude => _longitude;
        public double Latitude => _latitude;
    }

    private class Vehicle
        : IVehicleInfo
    {
        [JsonProperty("id")] private long _id;
        [JsonProperty("configuration")] private VehicleConfiguration? _configuration;

        public long Id => _id;
        public string Name => _configuration?.Name ?? "";
    }

    private class VehicleConfiguration
    {
        [JsonProperty("name")] private string? _name;
        [JsonProperty("family")] private string? _family;
        [JsonProperty("url")] private string? _url;

        public string Name => _name ?? "";
        public string Family => _family ?? "";
        public string Url => _url ?? "";
    }
}