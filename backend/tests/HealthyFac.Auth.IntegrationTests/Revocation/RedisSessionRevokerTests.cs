using HealthyFac.Auth.Infrastructure.Revocation;
using HealthyFac.Auth.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Testing.Caching;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace HealthyFac.Auth.IntegrationTests.Revocation;

public sealed class RedisSessionRevokerTests
{
    private readonly CountingTokenRevocationStore _store = new();
    private readonly RedisResiliencePipeline _redis = TestRedisPipeline.Create(TimeProvider.System);

    [Fact]
    public async Task RevokeAsync_WhenBreakerOpen_ThrowsWithoutStoreCall()
    {
        var revoker = new RedisSessionRevoker(_store, _redis, Options.Create(new RevocationOptions()));
        await _redis.OpenBreakerAsync();

        var act = () => revoker.RevokeAsync(
            SessionRevokerTestData.UserId,
            SessionRevokerTestData.RevokedAt,
            CancellationToken.None);

        await act.Should().ThrowAsync<BrokenCircuitException>();
        _store.RevokeCount.Should().Be(0);
    }

    [Fact]
    public async Task RevokeAsync_WhenBreakerClosed_WritesToStore()
    {
        var revoker = new RedisSessionRevoker(_store, _redis, Options.Create(new RevocationOptions()));

        await revoker.RevokeAsync(
            SessionRevokerTestData.UserId,
            SessionRevokerTestData.RevokedAt,
            CancellationToken.None);

        _store.RevokeCount.Should().Be(1);
    }
}
