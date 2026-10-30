using HealthyFac.Auth.Application.Identities;
using HealthyFac.Auth.IntegrationTests.Identities.Data;
using HealthyFac.Auth.IntegrationTests.Keycloak.Data;

namespace HealthyFac.Auth.IntegrationTests.Identities;

public sealed class ProvisionIdentityTests(ProvisioningFixture fixture) : IClassFixture<ProvisioningFixture>
{
    private readonly ProvisioningFixture _fixture = fixture;

    [Fact]
    public async Task Provision_WithSeededOrganization_CreatesMemberWithRoleAndPublishesProvisioned()
    {
        var message = ProvisioningTestData.CreateAccepted();

        await _fixture.ProvisionAsync(message);

        var user = await _fixture.LoadProvisionedUserAsync(message.EmployeeId, message.CompanyId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);
        var mailCount = await _fixture.CountMailsToAsync(message.Email);

        user.UserId.Should().NotBeNull();
        user.IsOrganizationMember.Should().BeTrue();
        user.RoleNames.Should().Contain(message.RoleCode);
        provisioned.Should().ContainSingle().Which.IdentityUserId.Should().Be(user.UserId.GetValueOrDefault());
        mailCount.Should().Be(1);
    }

    [Fact]
    public async Task Provision_Twice_CreatesOneUserAndPublishesSameIdentity()
    {
        var message = ProvisioningTestData.CreateAccepted();
        await _fixture.ProvisionAsync(message);

        await _fixture.ProvisionAsync(message);

        var userCount = await _fixture.CountUsersWithEmailAsync(message.Email);
        var userId = await _fixture.Keycloak.Admin.FindUserIdByEmployeeIdAsync(
            message.EmployeeId,
            CancellationToken.None);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        userCount.Should().Be(1);
        provisioned.Should().HaveCount(2);
        provisioned.Select(item => item.IdentityUserId).Should().OnlyContain(id => id == userId);
    }

    [Fact]
    public async Task Provision_WithUnknownCompany_PublishesFailedWithOrganizationNotFound()
    {
        var message = ProvisioningTestData.CreateAccepted(KeycloakAdminTestData.UnknownCompanyId);

        await _fixture.ProvisionAsync(message);

        var failed = await _fixture.LoadProvisioningFailedAsync(message.EmployeeId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);
        var userId = await _fixture.Keycloak.Admin.FindUserIdByEmployeeIdAsync(
            message.EmployeeId,
            CancellationToken.None);

        failed.Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(IdentityProvisioningFailureReasons.OrganizationNotFound);
        provisioned.Should().BeEmpty();
        userId.Should().BeNull();
    }

    [Fact]
    public async Task Provision_WithForeignUserOnSameEmail_PublishesFailed()
    {
        var message = ProvisioningTestData.CreateAccepted();
        await _fixture.CreateUserWithoutEmployeeIdAsync(message.Email);

        await _fixture.ProvisionAsync(message);

        var failed = await _fixture.LoadProvisioningFailedAsync(message.EmployeeId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);
        var mailCount = await _fixture.CountMailsToAsync(message.Email);

        failed.Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(IdentityProvisioningFailureReasons.EmailAlreadyRegistered);
        provisioned.Should().BeEmpty();
        mailCount.Should().Be(0);
    }

    [Fact]
    public async Task Provision_WithUnknownRole_PublishesFailedWithRoleNotFound()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var identityAdmin = new FakeIdentityAdmin { IsRealmRoleMissing = true };

        await _fixture.ProvisionAsync(message, identityAdmin);

        var failed = await _fixture.LoadProvisioningFailedAsync(message.EmployeeId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        failed.Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(IdentityProvisioningFailureReasons.RoleNotFound);
        provisioned.Should().BeEmpty();
        identityAdmin.PasswordEmailUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Provision_WhenKeycloakUnavailable_ThrowsForRetry()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var identityAdmin = new FakeIdentityAdmin { IsUnavailable = true };

        var act = () => _fixture.ProvisionAsync(message, identityAdmin);

        await act.Should().ThrowAsync<HttpRequestException>();
        var failed = await _fixture.LoadProvisioningFailedAsync(message.EmployeeId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        failed.Should().BeEmpty();
        provisioned.Should().BeEmpty();
    }

    [Fact]
    public async Task Provision_SameMessageDeliveredTwice_SendsSetPasswordEmailOnce()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var messageId = Guid.CreateVersion7();
        await _fixture.PublishAcceptedAsync(messageId, message);
        await _fixture.PublishAcceptedAsync(messageId, message);
        await _fixture.WaitForInboxMessageProcessedAsync(messageId);
        await Task.Delay(ProvisioningTestData.QuietPeriod);

        var inboxCount = await _fixture.CountInboxMessagesAsync(messageId);
        var mailCount = await _fixture.CountMailsToAsync(message.Email);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        inboxCount.Should().Be(1);
        mailCount.Should().Be(1);
        provisioned.Should().ContainSingle();
    }

    [Fact]
    public async Task Provision_RetryAfterUserCreated_SendsEmailWhileNoPassword()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var identityAdmin = new FakeIdentityAdmin();
        identityAdmin.FailOnceAfterCreate();
        var firstAttempt = () => _fixture.ProvisionAsync(message, identityAdmin);
        await firstAttempt.Should().ThrowAsync<HttpRequestException>();

        await _fixture.ProvisionAsync(message, identityAdmin);

        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        identityAdmin.CreatedUserCount.Should().Be(1);
        identityAdmin.PasswordEmailUserIds.Should().ContainSingle();
        provisioned.Should().ContainSingle();
    }

    [Fact]
    public async Task Provision_WhenParallelRunCreatedUser_ContinuesWithThatUser()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var identityAdmin = new FakeIdentityAdmin();
        identityAdmin.LoseCreateRaceOnce();

        await _fixture.ProvisionAsync(message, identityAdmin);

        var failed = await _fixture.LoadProvisioningFailedAsync(message.EmployeeId);
        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);
        var userId = await identityAdmin.FindUserIdByEmployeeIdAsync(message.EmployeeId, CancellationToken.None);

        failed.Should().BeEmpty();
        provisioned.Should().ContainSingle().Which.IdentityUserId.Should().Be(userId.GetValueOrDefault());
        identityAdmin.PasswordEmailUserIds.Should().ContainSingle().Which.Should().Be(userId.GetValueOrDefault());
    }

    [Fact]
    public async Task Provision_ForUserWithPassword_SendsNoEmail()
    {
        var message = ProvisioningTestData.CreateAccepted();
        var identityAdmin = new FakeIdentityAdmin();
        identityAdmin.AddUserWithPassword(message.EmployeeId);

        await _fixture.ProvisionAsync(message, identityAdmin);

        var provisioned = await _fixture.LoadProvisionedAsync(message.EmployeeId);

        identityAdmin.PasswordEmailUserIds.Should().BeEmpty();
        provisioned.Should().ContainSingle();
    }
}
