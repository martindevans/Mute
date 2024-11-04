using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Weather;

public class OpenWeatherMapService
    : IWeather
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private readonly JsonSerializerOptions _serializer;

    public OpenWeatherMapService(Configuration config, IHttpClientFactory http)
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

    public async Task<IWeatherReport?> GetCurrentWeather(float latitude, float longitude)
    {
        var result = await _http.GetAsync($"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&units=metric&appid={_apiKey}");
        if (!result.IsSuccessStatusCode)
            return null;

        var json = JsonSerializer.Deserialize<WeatherResponse>(await result.Content.ReadAsStreamAsync(), _serializer);
        if (json == null)
            return null;

        return new WeatherReport(json);
    }

    private class WeatherResponse
    {
        public required WeatherResponsePos Coord { get; init; }
        public required WeatherResponseWeather[] Weather { get; init; }
        public required WeatherResponseMain Main { get; init; }
        public required WeatherResponseWind Wind { get; init; }
        public WeatherResponseRain? Rain { get; init; }
    }

    private record WeatherResponsePos(float Lat, float Lon);
    private record WeatherResponseWeather(string Main, string Description);
    private record WeatherResponseMain(float Temp, [property: JsonPropertyName("feels_like")] float FeelsLike, float TempMin, float TempMax, float Pressure, float Humidity, float SeaLevel, float GrndLevel);
    private record WeatherResponseWind(float Speed, float Deg, float Gust);
    private record WeatherResponseRain([property: JsonPropertyName("1h")] float oneHour);

    private class WeatherReport
        : IWeatherReport
    {
        private readonly WeatherResponse _weather;

        public WeatherReport(WeatherResponse weather)
        {
            _weather = weather;

            List<string> parts =
            [
                ..weather.Weather.Select(a => a.Description),
                $"feels like {weather.Main.FeelsLike}°C"
            ];
            if (weather.Rain is { oneHour: var mm })
                parts.Add($"with {mm}mm of rain per hour");
            Description = string.Join(", ", parts);
        }

        public string Description { get; }
        public float Temperature => _weather.Main.Temp;
        public float? TemperatureFeelsLike => _weather.Main.FeelsLike;
        public float? WindSpeed => _weather.Wind.Speed;
        public float? WindBearing => _weather.Wind.Deg;
        public float? RainMM => _weather.Rain?.oneHour ?? 0;
    }
}