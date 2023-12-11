using System.Threading.Tasks;

namespace Mute.Moe.Services.RateLimit;

public interface IRateLimit
{
    public Task<RateLimitState?> TryGetLastUsed(Guid rateId, ulong userId);

    public Task Use(Guid rateId, ulong userId);

    public Task Reset(Guid rateId, ulong userId);
}

public readonly record struct RateLimitState(DateTime LastUsed, uint UseCount);