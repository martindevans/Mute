using System.Threading.Tasks;

namespace Mute.Moe.Services.RateLimit;

/// <summary>
/// Service for storing and updating rate limits
/// </summary>
public interface IRateLimit
{
    /// <summary>
    /// Get the last time the given rate limited resource was used by the given user ID
    /// </summary>
    /// <param name="rateId">ID of the resource</param>
    /// <param name="userId">ID of the user</param>
    /// <returns></returns>
    public Task<RateLimitState?> TryGetLastUsed(Guid rateId, ulong userId);

    /// <summary>
    /// "Use" the given resource ID by the given user
    /// </summary>
    /// <param name="rateId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task Use(Guid rateId, ulong userId);

    /// <summary>
    /// Reset the rate limit for a user and resource
    /// </summary>
    /// <param name="rateId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task Reset(Guid rateId, ulong userId);
}

/// <summary>
/// The current state of a rate limit
/// </summary>
/// <param name="LastUsed"></param>
/// <param name="UseCount"></param>
public readonly record struct RateLimitState(DateTime LastUsed, uint UseCount);