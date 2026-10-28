using HealthyFac.Shared.UnitTests.Authentication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Authentication;

public sealed class PermissionPoliciesTests
{
    [Fact]
    public async Task Authorize_WithPermissionInPermissionsClaim_Succeeds()
    {
        await using var provider = PermissionPoliciesTestData.CreateProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = PermissionPoliciesTestData.CreateUser(PermissionPoliciesTestData.InvitePermission);

        var result = await authorization.AuthorizeAsync(user, null, PermissionPoliciesTestData.InvitePermission);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WithOtherPermissionInPermissionsClaim_Fails()
    {
        await using var provider = PermissionPoliciesTestData.CreateProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = PermissionPoliciesTestData.CreateUser(PermissionPoliciesTestData.MachinesPermission);

        var result = await authorization.AuthorizeAsync(user, null, PermissionPoliciesTestData.InvitePermission);

        result.Succeeded.Should().BeFalse();
    }
}
