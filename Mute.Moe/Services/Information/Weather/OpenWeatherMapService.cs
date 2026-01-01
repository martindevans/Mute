using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluidCaching;

namespace Mute.Moe.Services.Information.Weather;

/// <inheritdoc />
public class OpenWeatherMapService
    : IWeather
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private readonly JsonSerializerOptions _serializer;

    private readonly FluidCache<IWeatherReport?> _currentWeatherCache;
    private readonly IIndex<(float Lat, float Lon), IWeatherReport?> _currentWeatherByLocation;

    private readonly FluidCache<IReadOnlyList<IWeatherForecast>?> _forecastWeatherCache;
    private readonly IIndex<(float, float), IReadOnlyList<IWeatherForecast>?> _forecastWeatherByLocation;

    /// <summary>
    /// Construct a new <see cref="OpenWeatherMapService"/>
    /// </summary>
    /// <param name="config"></param>
    /// <param name="http"></param>
    /// <exception cref="ArgumentNullException"></exception>
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

        _currentWeatherCache = new FluidCache<IWeatherReport?>(
            config.OpenWeatherMap.CacheSize,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(config.OpenWeatherMap.CacheMaxAgeSeconds),
            () => DateTime.UtcNow
        );
        _currentWeatherByLocation = _currentWeatherCache.AddIndex("ByLocation", a => (a?.Latitude ?? 0, a?.Longitude ?? 0));

        _forecastWeatherCache = new FluidCache<IReadOnlyList<IWeatherForecast>?>(
            config.OpenWeatherMap.CacheSize,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(config.OpenWeatherMap.CacheMaxAgeSeconds),
            () => DateTime.UtcNow
        );
        _forecastWeatherByLocation = _forecastWeatherCache.AddIndex("ByLocation", a => (a?[0].Latitude ?? 0, a?[0].Longitude ?? 0));
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public void Clear()
    {
        _currentWeatherCache.Clear();
        _forecastWeatherCache.Clear();
    }

    /// <inheritdoc />
    public async Task<IWeatherReport?> GetCurrentWeather(float latitude, float longitude)
    {
        return await _currentWeatherByLocation.GetItem((latitude, longitude), GetCurrentWeatherUncached);
    }

    private async Task<IWeatherReport?> GetCurrentWeatherUncached((float latitude, float longitude) loc)
    {
        var latitude = loc.latitude;
        var longitude = loc.longitude;

        var result = await _http.GetAsync($"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&units=metric&appid={_apiKey}");
        if (!result.IsSuccessStatusCode)
            return null;

        var json = JsonSerializer.Deserialize<WeatherResponse>(await result.Content.ReadAsStreamAsync(), _serializer);
        if (json == null)
            return null;

        return new WeatherReport(json);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IWeatherForecast>?> GetWeatherForecast(float latitude, float longitude)
    {
        return await _forecastWeatherByLocation.GetItem((latitude, longitude), GetForecastWeatherUncached);
    }

    private async Task<IReadOnlyList<IWeatherForecast>?> GetForecastWeatherUncached((float latitude, float longitude) loc)
    {
        var latitude = loc.latitude;
        var longitude = loc.longitude;

        var result = await _http.GetAsync($"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&appid={_apiKey}");
        if (!result.IsSuccessStatusCode)
            return null;

        var json = JsonSerializer.Deserialize<WeatherForecastResponse>(await result.Content.ReadAsStreamAsync(), _serializer);

        return json?
            .List
            .Select(a => new WeatherForecast(a, latitude, longitude))
            .ToArray();
    }

    #region report
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
        public float TemperatureCelsius => _weather.Main.Temp;
        public float? TemperatureCelsiusFeelsLike => _weather.Main.FeelsLike;
        public float? WindSpeedMetersPerSecond => _weather.Wind.Speed;
        public float? WindBearing => _weather.Wind.Deg;
        public float? RainMM => _weather.Rain?.oneHour ?? 0;

        public float Latitude => _weather.Coord.Lat;
        public float Longitude => _weather.Coord.Lon;
    }
    #endregion

    #region forecast
    private class WeatherForecastResponse
    {
        public required WeatherForecastResponseListItem[] List { get; init; }
    }

    private class WeatherForecastResponseListItem
    {
        [JsonPropertyName("dt")]
        public required ulong UnixTimestamp { get; init; }

        [JsonPropertyName("main")]
        public required WeatherForecastResponseMain Main { get; init; }

        [JsonPropertyName("weather")]
        public required WeatherForecastResponseWeatherItem[] Weather { get; init; }

        [JsonPropertyName("pop")]
        public required float ProbabilityOfPrecipitation { get; init; }

        public DateTime Timestamp => UnixTimestamp.FromUnixTimestamp();
    }

    private record WeatherForecastResponseMain(
        [property: JsonPropertyName("temp")] float Temp,
        [property: JsonPropertyName("feels_like")] float FeelsLike,
        [property: JsonPropertyName("temp_min")] float MinForecastTemp,
        [property: JsonPropertyName("temp_max")] float MaxForecastTemp
    );

    private record WeatherForecastResponseWeatherItem(
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("main")] string Main
    );

    private class WeatherForecast
        : IWeatherForecast
    {
        private readonly WeatherForecastResponseListItem _weather;

        public float Latitude { get; }
        public float Longitude { get; }

        public WeatherForecast(WeatherForecastResponseListItem weather, float lat, float lon)
        {
            _weather = weather;

            Latitude = lat;
            Longitude = lon;

            Description = string.Join(", ", weather.Weather.Select(a => a.Description));
        }

        public string Description { get; }

        public float TemperatureCelsius => _weather.Main.Temp;
        public float? TemperatureCelsiusFeelsLike => _weather.Main.FeelsLike;

        public float ProbabilityOfPrecipitation => _weather.ProbabilityOfPrecipitation;
    }
    #endregion
}