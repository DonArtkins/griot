using StackExchange.Redis;

namespace Griot.Infrastructure.Redis;

/// <summary>
/// Sliding-window rate limiter backed by Redis.
/// Uses a sorted set where each member is a unique request timestamp.
/// Members older than the window are pruned on every check — O(log n) per request.
/// </summary>
public sealed class RedisRateLimiter : IRedisRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    // Lua script for atomic check-and-increment (EVALSHA avoids round-trips).
    // KEYS[1] = key, ARGV[1] = now (Unix ms), ARGV[2] = window start (Unix ms), ARGV[3] = limit, ARGV[4] = TTL seconds
    private const string SlidingWindowScript = """
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window_start = tonumber(ARGV[2])
        local limit = tonumber(ARGV[3])
        local ttl = tonumber(ARGV[4])
        -- Remove expired entries
        redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)
        -- Count current entries
        local count = redis.call('ZCARD', key)
        if count < limit then
            -- Add current request
            redis.call('ZADD', key, now, now .. '-' .. math.random(1,1000000))
            redis.call('EXPIRE', key, ttl)
            return {1, limit - count - 1}
        else
            -- Get oldest member to compute retry-after
            local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
            local retry_ms = 0
            if #oldest >= 2 then
                retry_ms = math.max(0, tonumber(oldest[2]) - window_start)
            end
            return {0, 0, retry_ms}
        end
        """;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RateLimitResult> TryAcquireAsync(string key, int limit, int windowSeconds)
    {
        var db = _redis.GetDatabase(0, null);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStartMs = nowMs - (windowSeconds * 1000L);

        var rawResult = await db.ScriptEvaluateAsync(
            SlidingWindowScript,
            new RedisKey[] { key },
            new RedisValue[] { nowMs, windowStartMs, limit, windowSeconds + 5 /* buffer for TTL */ },
            CommandFlags.None
        );

        var result = (RedisResult[]?)rawResult ?? new RedisResult[]
        {
            RedisResult.Create((RedisValue)0),
            RedisResult.Create((RedisValue)0)
        };

        var allowed = (int)result[0] == 1;
        var remaining = allowed ? (int)result[1] : 0;
        var retryAfterMs = allowed ? 0L : (result.Length > 2 ? (long)result[2] : windowSeconds * 1000L);
        var retryAfter = TimeSpan.FromMilliseconds(Math.Max(retryAfterMs, allowed ? 0L : 1000L));

        return new RateLimitResult(allowed, remaining, retryAfter);
    }
}
