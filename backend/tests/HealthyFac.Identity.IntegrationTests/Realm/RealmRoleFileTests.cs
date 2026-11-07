using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

public sealed class RealmRoleFileTests
{
    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasMachinePermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName);

        permissions.Should().Contain(RealmTestData.MachinePermissions);
    }

    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasTelemetryPermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName, RealmTestData.TelemetryClientId);

        permissions.Should().BeEquivalentTo(RealmTestData.TelemetryPermissions);
    }
}
