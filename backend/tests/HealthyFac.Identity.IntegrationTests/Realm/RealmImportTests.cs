using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

public sealed class RealmImportTests(KeycloakRealmFixture fixture) : IClassFixture<KeycloakRealmFixture>
{
    private readonly KeycloakRealmFixture _fixture = fixture;

    [Fact]
    public async Task Import_WithPlaceholders_CreatesOrganizationWithCompanyAlias()
    {
        var organizations = await _fixture.ListOrganizationsAsync();

        organizations.Should()
            .ContainSingle()
            .Which.Alias.Should()
            .Be(RealmTestData.CompanyId.ToString());
    }

    [Fact]
    public async Task Import_WithPlaceholders_AddsSupervisorAsOrganizationMember()
    {
        var organizations = await _fixture.ListOrganizationsAsync();
        var memberIds = await _fixture.ListOrganizationMemberIdsAsync(organizations.Single().Id);

        memberIds.Should().Equal(RealmTestData.SupervisorIdentityId);
    }

    [Fact]
    public async Task PasswordGrant_ForSupervisor_TokenCarriesExpectedClaims()
    {
        var claims = await _fixture.GetSupervisorAccessTokenClaimsAsync();

        claims["sub"]!.GetValue<string>().Should().Be(RealmTestData.SupervisorIdentityId.ToString());
        claims["organization"]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .Equal(RealmTestData.CompanyId.ToString());
        claims[RealmTestData.EmployeeIdAttribute]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.SupervisorEmployeeId.ToString());
        claims["aud"]!.ToJsonString().Should().Contain(RealmTestData.CoreClientId);
        (claims["exp"]!.GetValue<long>() - claims["iat"]!.GetValue<long>())
            .Should()
            .Be(RealmTestData.AccessTokenLifespanSeconds);
    }

    [Fact]
    public async Task PasswordGrant_ForSupervisor_TokenCarriesPermissionsOnly()
    {
        var claims = await _fixture.GetSupervisorAccessTokenClaimsAsync();

        claims[RealmTestData.PermissionsClaim]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .BeEquivalentTo(RealmTestData.ExpectedPermissions)
            .And.NotContain(RealmTestData.SupervisorRoleName);
        claims.Select(claim => claim.Key).Should().NotContain(RealmTestData.RoleContainerClaims);
    }

    [Fact]
    public async Task ServiceAccountToken_HasClientIdWithoutClientHostOrAddress()
    {
        var claims = await _fixture.GetAuthServiceAccessTokenClaimsAsync();

        claims[RealmTestData.ClientIdClaim]!.GetValue<string>().Should().Be(RealmTestData.AuthClientId);
        claims.Select(claim => claim.Key).Should().NotContain(RealmTestData.RemovedServiceAccountClaims);
    }

    [Fact]
    public async Task UserProfile_EmployeeIdAttribute_IsNotEditableByUser()
    {
        var editRoles = await _fixture.ListEditRolesOfUserAttributeAsync(RealmTestData.EmployeeIdAttribute);

        editRoles.Should().Equal(RealmTestData.AdminRole);
    }
}
