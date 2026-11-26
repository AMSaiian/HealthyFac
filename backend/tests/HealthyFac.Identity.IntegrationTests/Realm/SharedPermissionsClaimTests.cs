using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Identity.IntegrationTests.Realm;

[Collection(SharedPermissionsClaimCollection.Name)]
[Feature(TestFeatures.Identity)]
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
