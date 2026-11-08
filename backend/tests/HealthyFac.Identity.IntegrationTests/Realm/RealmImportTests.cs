using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

[Collection(KeycloakRealmCollection.Name)]
public sealed class RealmImportTests(KeycloakRealmCollectionFixture fixture)
{
    private readonly KeycloakRealmFixture _fixture = fixture.Realm;

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

        claims[propertyName: "sub"]!.GetValue<string>().Should().Be(RealmTestData.SupervisorIdentityId.ToString());
        claims[propertyName: "organization"]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .Equal(RealmTestData.CompanyId.ToString());
        claims[RealmTestData.EmployeeIdAttribute]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.SupervisorEmployeeId.ToString());
        claims[propertyName: "aud"]!.ToJsonString().Should().Contain(RealmTestData.CoreClientId);
        (claims[propertyName: "exp"]!.GetValue<long>() - claims[propertyName: "iat"]!.GetValue<long>())
            .Should()
            .Be(RealmTestData.AccessTokenLifespanSeconds);
    }

    [Fact]
    public async Task UserToken_ForTestSupervisor_CarriesOrganizationAndEmployeeIdOnly()
    {
        var claims = await _fixture.GetSupervisorAccessTokenClaimsAsync();
        var claimNames = claims.Select(claim => claim.Key).ToList();

        claimNames.Should().Contain(["organization", RealmTestData.EmployeeIdAttribute]);
        claimNames.Should().NotContain(name => name.EndsWith(RealmTestData.IdListClaimSuffix));
    }

    [Fact]
    public async Task UserProfile_Always_DeclaresNoIdListAttribute()
    {
        var names = await _fixture.ListUserProfileAttributeNamesAsync();

        names.Should().Contain(RealmTestData.EmployeeIdAttribute);
        names.Should().NotContain(name => name.EndsWith(RealmTestData.IdListClaimSuffix));
    }

    [Fact]
    public async Task IdToken_ForTestSupervisor_CarriesPermissionsOnly()
    {
        var claims = await _fixture.GetSupervisorIdTokenClaimsAsync();

        claims[RealmTestData.PermissionsClaim]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .Contain(RealmTestData.MachinePermissions);
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
    public async Task PasswordGrant_ForSupervisor_TokenCarriesAudienceOfBothApis()
    {
        var claims = await _fixture.GetSupervisorAccessTokenClaimsAsync();

        claims.Audiences().Should().Contain([RealmTestData.CoreClientId, RealmTestData.TelemetryClientId]);
    }

    [Fact]
    public async Task ServiceAccountToken_HasClientIdWithoutClientHostOrAddress()
    {
        var claims = await _fixture.GetAuthServiceAccessTokenClaimsAsync();

        claims[RealmTestData.ClientIdClaim]!.GetValue<string>().Should().Be(RealmTestData.AuthClientId);
        claims.Select(claim => claim.Key)
            .Should()
            .NotContain(RealmTestData.RemovedServiceAccountClaims)
            .And.NotContain(RealmTestData.UserOnlyClaims);
    }

    [Fact]
    public async Task UserProfile_EmployeeIdAttribute_IsNotEditableByUser()
    {
        var editRoles = await _fixture.ListEditRolesOfUserAttributeAsync(RealmTestData.EmployeeIdAttribute);

        editRoles.Should().Equal(RealmTestData.AdminRole);
    }
}
