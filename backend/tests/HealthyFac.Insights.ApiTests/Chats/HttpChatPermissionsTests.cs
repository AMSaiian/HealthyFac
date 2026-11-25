using System.Security.Claims;
using HealthyFac.Insights.Web.Chats;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Testing.Features;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Insights.ApiTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class HttpChatPermissionsTests
{
    [Fact]
    public void Read_ForAUserWithPermissionClaims_ReturnsEachPermissionOnce()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(KeycloakClaimTypes.Permissions, value: "machines:read"),
                new Claim(KeycloakClaimTypes.Permissions, value: "cases:read"),
                new Claim(KeycloakClaimTypes.Permissions, value: "machines:read"),
                new Claim(ClaimTypes.Name, value: "not-a-permission"),
            ],
            authenticationType: "test");

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        var permissions = new HttpChatPermissions(accessor).Read();

        permissions.Should().BeEquivalentTo("machines:read", "cases:read");
    }

    [Fact]
    public void Read_OutsideARequest_ReturnsNoPermission()
    {
        var permissions = new HttpChatPermissions(new HttpContextAccessor()).Read();

        permissions.Should().BeEmpty();
    }
}
