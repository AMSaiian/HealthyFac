using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.Web.CurrentUser;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class HttpIncomingAccessTokenAccessorTests
{
    [Fact]
    public void TryGetUserAccessToken_WithUserToken_ReturnsToken()
    {
        var accessor = CreateAccessor(
            IncomingTokenTestData.CreateContext(
                ActorClaimsTestData.ValidPrincipal,
                IncomingTokenTestData.UserAccessToken));

        var token = accessor.TryGetUserAccessToken();
        var userId = accessor.TryGetUserId();

        token.Should().Be(IncomingTokenTestData.UserAccessToken);
        userId.Should().Be(ActorTestData.UserId);
    }

    [Fact]
    public void TryGetUserAccessToken_WithLowerCaseScheme_ReturnsToken()
    {
        var accessor = CreateAccessor(
            IncomingTokenTestData.CreateContext(
                ActorClaimsTestData.ValidPrincipal,
                IncomingTokenTestData.UserAccessToken,
                IncomingTokenTestData.LowerCaseBearerScheme));

        var token = accessor.TryGetUserAccessToken();

        token.Should().Be(IncomingTokenTestData.UserAccessToken);
    }

    [Fact]
    public void TryGetUserAccessToken_WithServiceToken_ReturnsNull()
    {
        var accessor = CreateAccessor(
            IncomingTokenTestData.CreateContext(
                IncomingTokenTestData.ServicePrincipal,
                IncomingTokenTestData.ServiceAccessToken));

        var token = accessor.TryGetUserAccessToken();
        var userId = accessor.TryGetUserId();

        token.Should().BeNull();
        userId.Should().BeNull();
    }

    [Fact]
    public void TryGetUserAccessToken_OutsideHttpRequest_ReturnsNull()
    {
        var accessor = CreateAccessor(context: null);

        var token = accessor.TryGetUserAccessToken();

        token.Should().BeNull();
    }

    private static HttpIncomingAccessTokenAccessor CreateAccessor(HttpContext? context) =>
        new(new HttpContextAccessor { HttpContext = context });
}
