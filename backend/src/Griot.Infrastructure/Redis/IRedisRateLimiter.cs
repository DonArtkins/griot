namespace Griot.Infrastructure.Redis;

/// <summary>
/// Redis-backed sliding-window rate limiter.
/// Used by the auth endpoints and GraphQL query-cost guard.
/// </summary>
public interface IRedisRateLimiter
{
    /// <summary>
    /// Attempt to consume one permit from the sliding window for <paramref name="key"/>.
    /// Returns true when the request is allowed; false when the limit is exceeded.
    /// </summary>
    /// <param name="key">Partition key (e.g. "login:{ip}").</param>
    /// <param name="limit">Max permits per window.</param>
    /// <param name="windowSeconds">Sliding window duration in seconds.</param>
    Task<RateLimitResult> TryAcquireAsync(string key, int limit, int windowSeconds);
}

public record RateLimitResult(bool Allowed, int Remaining, TimeSpan RetryAfter);
