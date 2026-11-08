using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

[Collection(SharedPermissionsClaimCollection.Name)]
public sealed class SharedPermissionsClaimTests(SharedPermissionsClaimFixture fixture)
{
    private readonly SharedPermissionsClaimFixture _fixture = fixture;

    [Fact]
    public async Task PasswordGrant_WithTwoPermissionMappers_MergesPermissions()
    {
        var permissions = await _fixture.GetSupervisorPermissionsWithTwoMappersAsync();

        permissions.Should().BeEquivalentTo(SharedPermissionsClaimTestData.ExpectedMergedPermissions);
    }
}
