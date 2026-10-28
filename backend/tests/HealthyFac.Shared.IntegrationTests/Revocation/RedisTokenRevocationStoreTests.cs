using HealthyFac.Shared.IntegrationTests.Revocation.Data;

namespace HealthyFac.Shared.IntegrationTests.Revocation;

public sealed class RedisTokenRevocationStoreTests(RevocationStoreFixture fixture)
    : IClassFixture<RevocationStoreFixture>
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(6);

    private readonly RevocationStoreFixture _fixture = fixture;

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
