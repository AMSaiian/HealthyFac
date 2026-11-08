using HealthyFac.Shared.IntegrationTests.SharedRedis;
using HealthyFac.Shared.IntegrationTests.SharedRedis.Data;

namespace HealthyFac.Shared.IntegrationTests.Revocation;

[Collection(SharedRedisCollection.Name)]
public sealed class RedisTokenRevocationStoreTests(SharedRedisFixture fixture)
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(minutes: 6);

    private readonly SharedRedisFixture _fixture = fixture;

    [Fact]
    public async Task GetRevokedAt_WithoutEntry_ReturnsNull()
    {
        var revokedAt = await _fixture.Store.GetRevokedAtAsync(Guid.NewGuid(), CancellationToken.None);

        revokedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetRevokedAt_AfterRevoke_ReturnsTimeInSeconds()
    {
        var userId = Guid.NewGuid();
        var revokedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await _fixture.Store.RevokeAsync(userId, revokedAt, TimeToLive, CancellationToken.None);

        var stored = await _fixture.Store.GetRevokedAtAsync(userId, CancellationToken.None);

        stored.Should().Be(revokedAt);
    }
}
