using HealthyFac.Core.UnitTests.Authorization.Data;
using HealthyFac.Core.Web.Authorization;

namespace HealthyFac.Core.UnitTests.Authorization;

public sealed class CorePermissionsTests
{
    [Fact]
    public void All_ContainsEveryPermissionConstant()
    {
        var permissions = CorePermissions.All;

        permissions.Should().Contain(CorePermissionsTestData.Features.SelectMany(feature => feature.Constants));
        CorePermissionsTestData.Features.Should()
            .AllSatisfy(feature => feature.All.Should().Contain(feature.Constants, feature.Name));
    }

    [Fact]
    public void All_EqualsClientRolesOfCoreInRealmFile()
    {
        var permissions = CorePermissions.All;
        var clientRoles = RealmRolesTestData.ReadCoreClientRoles();

        permissions.Should().BeEquivalentTo(clientRoles);
    }

    [Fact]
    public void JobRoles_ReferenceOnlyKnownCorePermissions()
    {
        var permissions = CorePermissions.All;
        var jobRoles = RealmRolesTestData.ReadJobRoles();

        jobRoles.SelectMany(role => role.CorePermissions).Should().NotBeEmpty();
        jobRoles.Should()
            .AllSatisfy(role => role.CorePermissions.Should().BeSubsetOf(permissions, role.Name));
    }
}
