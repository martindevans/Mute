using Mute.Moe.Tools;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Geocoding;

/// <summary>
/// Geocoding service - convert string query to lat/long
/// </summary>
public interface IGeocoding
{
    /// <summary>
    /// Given a query of EITHER city_name or country_code return a list of the most likely locations (latitude and longitude)
    /// </summary>
    /// <param name="query">The location to find. EITHER city_name or country_code.</param>
    /// <returns></returns>
    public Task<IReadOnlyList<GeocodingResponse>> LookupLocation(string query);

    /// <summary>
    /// Response from a geocoding query
    /// </summary>
    public record GeocodingResponse
    {
        /// <summary>
        /// Latitude of this result
        /// </summary>
        public required double Latitude { get; init; }

        /// <summary>
        /// Longitude of this result
        /// </summary>
        public required double Longitude { get; init; }

        /// <summary>
        /// Country this result is in
        /// </summary>
        public required string Country { get; init; }

        /// <summary>
        /// Canonical name of this result
        /// </summary>
        public required string Name { get; init; }
    }
}

/// <summary>
/// Provides geocoding related tools
/// </summary>
public class GeocodingToolProvider
    : IToolProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Construct a new <see cref="GeocodingToolProvider"/>
    /// </summary>
    /// <param name="geocoding"></param>
    public GeocodingToolProvider(IGeocoding geocoding)
    {
        Tools =
        [
            new AutoTool("geocoding_lookup", false, geocoding.LookupLocation)
        ];
    }
}