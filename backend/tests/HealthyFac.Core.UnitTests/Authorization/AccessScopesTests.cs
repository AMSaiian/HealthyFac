using HealthyFac.Core.UnitTests.Authorization.Data;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Authorization;

[Feature(TestFeatures.Identity)]
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
