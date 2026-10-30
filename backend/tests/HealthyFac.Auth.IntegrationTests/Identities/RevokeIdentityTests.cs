using HealthyFac.Auth.IntegrationTests.Identities.Data;
using HealthyFac.Auth.IntegrationTests.Keycloak.Data;
using StackExchange.Redis;

namespace HealthyFac.Auth.IntegrationTests.Identities;

public sealed class RevokeIdentityTests(ProvisioningFixture fixture) : IClassFixture<ProvisioningFixture>
{
    private readonly ProvisioningFixture _fixture = fixture;

    [Fact]
    public async Task Revoke_WithIdentity_WritesEntryDisablesAndLogsOut()
    {
        var accepted = ProvisioningTestData.CreateAccepted();
        await _fixture.ProvisionAsync(accepted);
        var user = await _fixture.LoadProvisionedUserAsync(accepted.EmployeeId, accepted.CompanyId);
        var userId = user.UserId.GetValueOrDefault();
        var message = ProvisioningTestData.CreateDeactivated(userId, accepted.CompanyId);

        await _fixture.RevokeAsync(message);

        var entry = await _fixture.LoadRevocationAsync(userId);
        var isEnabled = await _fixture.Keycloak.IsUserEnabledAsync(userId);

        entry.Should().NotBeNull();
        entry.TimeToLive.Should().BeGreaterThan(ProvisioningTestData.MinimumRevocationTimeToLive);
        entry.TimeToLive.Should().BeLessThanOrEqualTo(ProvisioningTestData.ExpectedRevocationTimeToLive);
        isEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_WhenDisableFails_RetriesAndRevokesAfterLogout()
    {
        var userId = Guid.CreateVersion7();
        var message = ProvisioningTestData.CreateDeactivated(userId);
        var identityAdmin = new FakeIdentityAdmin();
        identityAdmin.FailOnceOnDisable();
        var firstAttempt = () => _fixture.RevokeAsync(message, identityAdmin);
        await firstAttempt.Should().ThrowAsync<HttpRequestException>();
        var entryAfterFailure = await _fixture.LoadRevocationAsync(userId);

        await _fixture.RevokeAsync(message, identityAdmin);

        var entryAfterRetry = await _fixture.LoadRevocationAsync(userId);

        entryAfterFailure.Should().BeNull();
        entryAfterRetry.Should().NotBeNull();
        identityAdmin.DisabledUserIds.Should().ContainSingle().Which.Should().Be(userId);
        identityAdmin.LoggedOutUserIds.Should().ContainSingle().Which.Should().Be(userId);
    }

    [Fact]
    public async Task Revoke_WhenRedisFails_DisablesLogsOutAndThrowsForRetry()
    {
        var userId = Guid.CreateVersion7();
        var message = ProvisioningTestData.CreateDeactivated(userId);
        var identityAdmin = new FakeIdentityAdmin();
        var sessionRevoker = new FailingSessionRevoker();

        var act = () => _fixture.RevokeAsync(message, identityAdmin, sessionRevoker);

        await act.Should().ThrowAsync<RedisException>();
        identityAdmin.DisabledUserIds.Should().ContainSingle().Which.Should().Be(userId);
        identityAdmin.LoggedOutUserIds.Should().ContainSingle().Which.Should().Be(userId);
    }

    [Fact]
    public async Task Revoke_WithIdentity_StoresRevocationTimeAfterLogout()
    {
        var message = ProvisioningTestData.CreateDeactivated(Guid.CreateVersion7()) with
        {
            DeactivatedAt = DateTime.UtcNow.AddMinutes(-ProvisioningTestData.DelayedDeactivationMinutes),
        };
        var identityAdmin = new FakeIdentityAdmin();
        var beforeAct = DateTimeOffset.UtcNow;

        await _fixture.RevokeAsync(message, identityAdmin);

        var entry = await _fixture.LoadRevocationAsync(message.IdentityUserId.GetValueOrDefault());

        entry.Should().NotBeNull();
        entry!.RevokedAt.ToUnixTimeSeconds().Should().BeGreaterThanOrEqualTo(beforeAct.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Revoke_WithoutIdentity_Completes()
    {
        var message = ProvisioningTestData.CreateDeactivated(identityUserId: null);
        var identityAdmin = new FakeIdentityAdmin();

        await _fixture.RevokeAsync(message, identityAdmin);

        identityAdmin.DisabledUserIds.Should().BeEmpty();
    }
}
