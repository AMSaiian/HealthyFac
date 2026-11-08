using HealthyFac.Shared.IntegrationTests.ServiceTokens.Data;
using HealthyFac.Shared.IntegrationTests.SharedRedis;
using HealthyFac.Shared.IntegrationTests.SharedRedis.Data;

namespace HealthyFac.Shared.IntegrationTests.ServiceTokens;

[Collection(SharedRedisCollection.Name)]
public sealed class RedisTokenCacheStorageTests(SharedRedisFixture fixture)
{
    private readonly SharedRedisFixture _fixture = fixture;

    [Fact]
    public async Task RedisTokenCacheStorage_SetThenGet_ReturnsValue()
    {
        var key = TokenStorageTestData.NewKey();
        await _fixture.Storage.SetAsync(
            key,
            TokenStorageTestData.StoredValue,
            TokenStorageTestData.TimeToLive,
            CancellationToken.None);

        var value = await _fixture.Storage.GetAsync(key, CancellationToken.None);

        value.Should().Be(TokenStorageTestData.StoredValue);
    }

    [Fact]
    public async Task RedisTokenCacheStorage_Set_UsesTimeToLive()
    {
        var key = TokenStorageTestData.NewKey();

        await _fixture.Storage.SetAsync(
            key,
            TokenStorageTestData.StoredValue,
            TokenStorageTestData.TimeToLive,
            CancellationToken.None);

        var timeToLive = await _fixture.ReadTimeToLiveAsync(key);

        timeToLive.Should().NotBeNull();
        timeToLive!.Value.Should().BePositive().And.BeLessThanOrEqualTo(TokenStorageTestData.TimeToLive);
    }

    [Fact]
    public async Task RedisTokenCacheStorage_TryAcquireLock_SecondOwnerFails()
    {
        var key = TokenStorageTestData.NewKey();
        var isFirstAcquired = await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.FirstOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        var isSecondAcquired = await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.SecondOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        isFirstAcquired.Should().BeTrue();
        isSecondAcquired.Should().BeFalse();
    }

    [Fact]
    public async Task RedisTokenCacheStorage_ReleaseLock_OtherOwnerKeepsLock()
    {
        var key = TokenStorageTestData.NewKey();
        await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.FirstOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        await _fixture.Storage.ReleaseLockAsync(key, TokenStorageTestData.SecondOwner, CancellationToken.None);

        var isAcquiredAfterRelease = await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.SecondOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        isAcquiredAfterRelease.Should().BeFalse();
    }

    [Fact]
    public async Task RedisTokenCacheStorage_ReleaseLock_OwnerRemovesLock()
    {
        var key = TokenStorageTestData.NewKey();
        await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.FirstOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        await _fixture.Storage.ReleaseLockAsync(key, TokenStorageTestData.FirstOwner, CancellationToken.None);

        var isAcquiredAfterRelease = await _fixture.Storage.TryAcquireLockAsync(
            key,
            TokenStorageTestData.SecondOwner,
            TokenStorageTestData.LockTimeToLive,
            CancellationToken.None);

        isAcquiredAfterRelease.Should().BeTrue();
    }
}
