using HealthyFac.Core.UnitTests.Authorization.Data;
using HealthyFac.Core.Web.Authorization;

namespace HealthyFac.Core.UnitTests.Authorization;

public sealed class AccessScopesTests
{
    [Theory]
    [MemberData(nameof(AccessScopesTestData.Cases), MemberType = typeof(AccessScopesTestData))]
    public void Resolve_WithPermissions_ReturnsScopeOfTheManagePermission(
        string managePermission,
        string[] permissions,
        string expectedScope
    )
    {
        var scope = AccessScopes.Resolve(AccessScopesTestData.UserWith(permissions), managePermission);

        scope.Value.Should().Be(expectedScope);
    }
}
