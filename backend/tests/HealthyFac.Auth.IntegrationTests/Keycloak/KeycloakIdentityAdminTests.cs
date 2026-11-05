using System.Net;
using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Auth.Application.Identities;
using HealthyFac.Auth.IntegrationTests.Identities.Data;
using HealthyFac.Auth.IntegrationTests.Keycloak.Data;

namespace HealthyFac.Auth.IntegrationTests.Keycloak;

[Collection(KeycloakCollection.Name)]
public sealed class KeycloakIdentityAdminTests(ProvisioningFixture fixture)
{
    private readonly KeycloakAdminFixture _fixture = fixture.Keycloak;

    [Fact]
    public async Task FindOrganizationId_ForSeededCompany_ReturnsId()
    {
        var expectedId = await _fixture.LoadOrganizationIdAsync(KeycloakAdminTestData.CompanyId);

        var organizationId = await _fixture.Admin.FindOrganizationIdAsync(
            KeycloakAdminTestData.CompanyId,
            CancellationToken.None);

        expectedId.Should().NotBeNull();
        organizationId.Should().Be(expectedId);
    }

    [Fact]
    public async Task FindOrganizationId_ForUnknownCompany_ReturnsNull()
    {
        var organizationId = await _fixture.Admin.FindOrganizationIdAsync(
            KeycloakAdminTestData.UnknownCompanyId,
            CancellationToken.None);

        organizationId.Should().BeNull();
    }

    [Fact]
    public async Task CreateUser_ThenFindByEmployeeId_ReturnsSameId()
    {
        var identity = KeycloakAdminTestData.CreateIdentity();
        var userId = await _fixture.Admin.CreateUserAsync(identity, CancellationToken.None);

        var foundId = await _fixture.Admin.FindUserIdByEmployeeIdAsync(identity.EmployeeId, CancellationToken.None);

        foundId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateUser_WithExistingEmail_ThrowsIdentityConflict()
    {
        var first = KeycloakAdminTestData.CreateIdentity();
        await _fixture.Admin.CreateUserAsync(first, CancellationToken.None);
        var second = KeycloakAdminTestData.CreateIdentity() with { Email = first.Email };

        var act = () => _fixture.Admin.CreateUserAsync(second, CancellationToken.None);

        await act.Should().ThrowAsync<IdentityConflictException>();
    }

    [Fact]
    public async Task HasPassword_ForNewUser_ReturnsFalse()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);

        var hasPassword = await _fixture.Admin.HasPasswordAsync(userId, CancellationToken.None);

        hasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task HasPassword_AfterPasswordSet_ReturnsTrue()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);
        await _fixture.SetPasswordAsync(userId, KeycloakAdminTestData.NewPassword);

        var hasPassword = await _fixture.Admin.HasPasswordAsync(userId, CancellationToken.None);

        hasPassword.Should().BeTrue();
    }

    [Fact]
    public async Task AddOrganizationMember_Twice_DoesNotThrow()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);
        var organizationId = await _fixture.LoadOrganizationIdAsync(KeycloakAdminTestData.CompanyId);
        await _fixture.Admin.AddOrganizationMemberAsync(organizationId!.Value, userId, CancellationToken.None);

        await _fixture.Admin.AddOrganizationMemberAsync(organizationId.Value, userId, CancellationToken.None);

        var memberIds = await _fixture.LoadOrganizationMemberIdsAsync(organizationId.Value);

        memberIds.Count(memberId => memberId == userId).Should().Be(1);
    }

    [Fact]
    public async Task AssignRealmRole_Twice_KeepsOneMapping()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);
        await _fixture.Admin.AssignRealmRoleAsync(userId, KeycloakAdminTestData.WorkerRoleName, CancellationToken.None);

        await _fixture.Admin.AssignRealmRoleAsync(userId, KeycloakAdminTestData.WorkerRoleName, CancellationToken.None);

        var roleNames = await _fixture.LoadRealmRoleNamesAsync(userId);

        roleNames.Count(roleName => roleName == KeycloakAdminTestData.WorkerRoleName).Should().Be(1);
    }

    [Fact]
    public async Task AssignRealmRole_ForUnknownRole_ThrowsRoleNotFound()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);

        var act = () => _fixture.Admin.AssignRealmRoleAsync(
            userId,
            KeycloakAdminTestData.UnknownRoleName,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<RealmRoleNotFoundException>()
            .WithMessage($"*{KeycloakAdminTestData.UnknownRoleName}*");
    }

    [Fact]
    public async Task SendSetPasswordEmail_ForUser_ArrivesInMailpit()
    {
        var identity = KeycloakAdminTestData.CreateIdentity();
        var userId = await _fixture.Admin.CreateUserAsync(identity, CancellationToken.None);

        await _fixture.Admin.SendSetPasswordEmailAsync(userId, CancellationToken.None);

        var message = await _fixture.WaitForMailToAsync(identity.Email);

        message.To.Should().ContainSingle().Which.Should().Be(identity.Email);
    }

    [Fact]
    public async Task SendSetPasswordEmail_WithRedirectUriNotAllowed_ThrowsWithPathAndReason()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);
        var admin = _fixture.CreateAdmin(KeycloakAdminTestData.UnregisteredSpaUrl);

        var act = () => admin.SendSetPasswordEmailAsync(userId, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should()
            .Contain($"{userId}/execute-actions-email")
            .And.Contain(KeycloakAdminTestData.RedirectUriRejection)
            .And.NotContain(KeycloakAdminTestData.UnregisteredSpaUrl);
    }

    [Fact]
    public async Task DisableAndLogout_ForUser_DisablesUser()
    {
        var userId = await _fixture.Admin.CreateUserAsync(
            KeycloakAdminTestData.CreateIdentity(),
            CancellationToken.None);
        var wasEnabledBefore = await _fixture.IsUserEnabledAsync(userId);

        await _fixture.Admin.DisableUserAsync(userId, CancellationToken.None);
        await _fixture.Admin.LogoutUserAsync(userId, CancellationToken.None);

        var isEnabled = await _fixture.IsUserEnabledAsync(userId);

        wasEnabledBefore.Should().BeTrue();
        isEnabled.Should().BeFalse();
    }
}
