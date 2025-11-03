using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Mute.Moe.Services.Information.Geocoding.IGeocoding;

namespace Mute.Moe.Services.Information.Geocoding;

/// <summary>
/// Use open weather map API for geocoding
/// </summary>
public class OpenWeatherMapGeocoding
    : IGeocoding
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private readonly JsonSerializerOptions _serializer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="http"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public OpenWeatherMapGeocoding(Configuration config, IHttpClientFactory http)
    {
        _apiKey = config.OpenWeatherMap?.ApiKey
               ?? throw new ArgumentNullException(nameof(config.OpenWeatherMap.ApiKey));

        _http = http.CreateClient();

        _serializer = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeocodingResponse>> LookupLocation(string query)
    {
        var result = await _http.GetAsync($"http://api.openweathermap.org/geo/1.0/direct?q={query}&limit={5}&appid={_apiKey}");
        if (!result.IsSuccessStatusCode)
            return [ ];

        var response = JsonSerializer.Deserialize<GeocodingResponseItem[]>(await result.Content.ReadAsStreamAsync(), _serializer);

        return (from item in response
                select new GeocodingResponse
                {
                    Name = item.Name,
                    Country = $"{item.Country ?? "unknown"} ({item.State ?? "unknown"})",

                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                }).ToArray();
    }

    [UsedImplicitly]
    private class GeocodingResponseItem
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        //[JsonPropertyName("local_names")]
        //public required Dictionary<string, string> LocalNames { get; init; }

        [JsonPropertyName("lat")]
        public required double Latitude { get; init; }

        [JsonPropertyName("lon")]
        public required double Longitude { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }
    }
}