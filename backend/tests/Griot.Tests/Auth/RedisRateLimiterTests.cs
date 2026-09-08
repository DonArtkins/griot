using Griot.Infrastructure.Redis;
using Moq;
using StackExchange.Redis;

namespace Griot.Tests.Auth;

/// <summary>
/// Unit tests for <see cref="RedisRateLimiter"/>.
/// Tests use a mock IConnectionMultiplexer so no live Redis is required.
/// </summary>
public class RedisRateLimiterTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Allowed — under the limit
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TryAcquire_UnderLimit_ReturnsAllowed()
    {
        // Arrange: Lua script returns {1, <remaining>} when allowed.
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]?>(),
                It.IsAny<RedisValue[]?>(),
                It.IsAny<CommandFlags>()))
              .ReturnsAsync(RedisResult.Create(new RedisResult[]
              {
                  RedisResult.Create((RedisValue)1),
                  RedisResult.Create((RedisValue)9)
              }));

        var connMock = new Mock<IConnectionMultiplexer>();
        connMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(dbMock.Object);

        var sut = new RedisRateLimiter(connMock.Object);

        // Act
        var result = await sut.TryAcquireAsync("test:key", limit: 10, windowSeconds: 900);

        // Assert
        Assert.True(result.Allowed);
        Assert.Equal(9, result.Remaining);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Blocked — over the limit (429 scenario)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TryAcquire_OverLimit_ReturnsBlocked_With_RetryAfter()
    {
        // Arrange: Lua script returns {0, 0, <retry_ms>} when over limit.
        long retryMs = 60_000; // 60 seconds

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]?>(),
                It.IsAny<RedisValue[]?>(),
                It.IsAny<CommandFlags>()))
              .ReturnsAsync(RedisResult.Create(new RedisResult[]
              {
                  RedisResult.Create((RedisValue)0),
                  RedisResult.Create((RedisValue)0),
                  RedisResult.Create((RedisValue)retryMs)
              }));

        var connMock = new Mock<IConnectionMultiplexer>();
        connMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(dbMock.Object);

        var sut = new RedisRateLimiter(connMock.Object);

        // Act
        var result = await sut.TryAcquireAsync("test:key", limit: 10, windowSeconds: 900);

        // Assert
        Assert.False(result.Allowed);
        Assert.Equal(0, result.Remaining);
        Assert.True(result.RetryAfter.TotalMilliseconds > 0);
    }
}
