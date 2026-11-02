using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.UnitTests.Authorization.Data;

namespace HealthyFac.Core.UnitTests.Authorization;

public sealed class RoleCodesTests
{
    [Fact]
    public void RoleCodes_EqualJobRolesInRealmFile()
    {
        var roleCodes = RoleCode.All.Select(roleCode => roleCode.Value);
        var jobRoles = RealmRolesTestData.ReadJobRoles()
            .Where(role => role.CorePermissions.Count > 0 || role.Name == RoleCode.Worker.Value)
            .Select(role => role.Name);

        roleCodes.Should().BeEquivalentTo(jobRoles);
    }
}
