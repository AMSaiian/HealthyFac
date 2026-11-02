using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;
using HealthyFac.Testing.Caching;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class RedisTokenCacheStorageTests
{
    [Fact]
    public async Task RedisTokenCacheStorage_WhenBreakerOpen_ThrowsStorageSkippedWithoutRedisCall()
    {
        var (connection, counter) = CallCountingConnection.Create();
        var redis = TestRedisPipeline.Create(new FakeTimeProvider());
        var storage = new RedisTokenCacheStorage(connection, redis);
        await redis.OpenBreakerAsync();

        var act = () => storage.GetAsync(ServiceTokenTestData.TokenKey, CancellationToken.None);

        await act.Should()
            .ThrowAsync<TokenStorageException>()
            .WithMessage(ServiceTokenErrors.StorageSkipped().Message);
        counter.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task RedisTokenCacheStorage_WhenBreakerClosed_CallsRedis()
    {
        var (connection, counter) = CallCountingConnection.Create();
        var storage = new RedisTokenCacheStorage(connection, TestRedisPipeline.Create(new FakeTimeProvider()));

        var act = () => storage.GetAsync(ServiceTokenTestData.TokenKey, CancellationToken.None);

        await act.Should().ThrowAsync<TokenStorageException>();
        counter.CallCount.Should().Be(1);
    }
}
