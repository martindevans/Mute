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
    /// Provides current weather conditions for a specified geographic location. Including temperature, precipitation and wind speed.<br />
    /// - Capability: weather data retrieval.<br />
    /// - Inputs: geographic location.<br />
    /// - Outputs: current conditions. Temperature, precipitation, wind.
    /// </summary>
    /// <param name="latitude">Latitude of query location</param>
    /// <param name="longitude">Longitude of query location</param>
    /// <returns></returns>
    public Task<IWeatherReport?> GetCurrentWeather(float latitude, float longitude);

    /// <summary>
    /// Provides a weather forecast for a specified geographic location. Including temperature and chance of precipitation.
    /// - Capability: weather forecast retrieval.<br />
    /// - Inputs: geographic location.<br />
    /// - Outputs: Predicted conditions. Temperature, precipitation chance.
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<IWeatherForecast>?> GetWeatherForecast(float latitude, float longitude);
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
/// Weather forecast for a location
/// </summary>
public interface IWeatherForecast
{
    /// <summary>
    /// Latitude for this forecast
    /// </summary>
    public float Latitude { get; }

    /// <summary>
    /// Longitude for this forecast
    /// </summary>
    public float Longitude { get; }

    /// <summary>
    /// Human readable description of this forecast
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Predicted temperature (C)
    /// </summary>
    public float TemperatureCelsius { get; }

    /// <summary>
    /// Human perception of predicted temperature (C)
    /// </summary>
    public float? TemperatureCelsiusFeelsLike { get; }

    /// <summary>
    /// Probability of precipitation (0 to 1)
    /// </summary>
    public float ProbabilityOfPrecipitation { get; }
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
            new AutoTool("get_current_weather", false, weather.GetCurrentWeather),
        ];
    }
}