using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Weather;

/// <summary>
/// Weather service - fetch weather reports
/// </summary>
public interface IWeather
{
    /// <summary>
    /// Get the weather report for a given location
    /// </summary>
    /// <param name="latitude">Longitude</param>
    /// <param name="longitude">Latitude</param>
    /// <returns></returns>
    public Task<IWeatherReport?> GetCurrentWeather(float latitude, float longitude);
}

/// <summary>
/// A weather report for a location
/// </summary>
public interface IWeatherReport
{
    /// <summary>
    /// Human readable description of current weather
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Current temperature (C)
    /// </summary>
    public float TemperatureCelsius { get; }

    /// <summary>
    /// Current human perception of temperature
    /// </summary>
    public float? TemperatureCelsiusFeelsLike { get; }

    /// <summary>
    /// Current wind speed (m/s)
    /// </summary>
    public float? WindSpeedMetersPerSecond { get; }

    /// <summary>
    /// Current wind bearing (degrees from north)
    /// </summary>
    public float? WindBearing { get; }

    /// <summary>
    /// Current rain (mm/hour)
    /// </summary>
    public float? RainMM { get; }

    /// <summary>
    /// Latitude for this report
    /// </summary>
    public float Latitude { get; }

    /// <summary>
    /// Longitude for this report
    /// </summary>
    public float Longitude { get; }
}

/// <summary>
/// Provide weather related tools for LLMs
/// </summary>
public class WeatherToolProvider
    : IToolProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="WeatherToolProvider"/>
    /// </summary>
    /// <param name="weather"></param>
    public WeatherToolProvider(IWeather weather)
    {
        Tools =
        [
            new AutoTool("get_weather", false, weather.GetCurrentWeather),
        ];
    }
}