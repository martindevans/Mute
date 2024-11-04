using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Weather;

public interface IWeather
{
    public Task<IWeatherReport?> GetCurrentWeather(float latitude, float longitude);
}

public interface IWeatherReport
{
    /// <summary>
    /// Human readable description of current weather
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Current temperature (C)
    /// </summary>
    public float Temperature { get; }

    /// <summary>
    /// Current human perception of temperature
    /// </summary>
    public float? TemperatureFeelsLike { get; }

    /// <summary>
    /// Current wind speed (m/s)
    /// </summary>
    public float? WindSpeed { get; }

    /// <summary>
    /// Current wind bearing (degrees from north)
    /// </summary>
    public float? WindBearing { get; }

    /// <summary>
    /// Current rain (mm/hour)
    /// </summary>
    public float? RainMM { get; }
}