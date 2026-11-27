using System.Security.Claims;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Testing.Features;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

[Feature(TestFeatures.Identity)]
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

    [Theory]
    [MemberData(nameof(NameClaimsTestData.NameClaims), MemberType = typeof(NameClaimsTestData))]
    public void TryGetUserName_WithSeveralNameClaims_ReadsNameThenPreferredUsernameThenEmail(
        string caseName,
        string firstType,
        string firstValue,
        string secondType,
        string secondValue,
        string expected
    )
    {
        var accessor = CreateAccessor(
            IncomingTokenTestData.CreateContext(
                PrincipalWith(new Claim(secondType, secondValue), new Claim(firstType, firstValue)),
                IncomingTokenTestData.UserAccessToken));

        var name = accessor.TryGetUserName();

        name.Should().Be(expected, caseName);
    }

    [Fact]
    public void TryGetUserName_WithServiceToken_ReturnsNull()
    {
        var accessor = CreateAccessor(
            IncomingTokenTestData.CreateContext(
                IncomingTokenTestData.ServicePrincipal,
                IncomingTokenTestData.ServiceAccessToken));

        var name = accessor.TryGetUserName();

        name.Should().BeNull();
    }

    private static ClaimsPrincipal PrincipalWith(params Claim[] extra) =>
        new(
            new ClaimsIdentity(
                ActorClaimsTestData.ValidPrincipal.Claims.Concat(extra),
                authenticationType: "test"));

    private static HttpIncomingAccessTokenAccessor CreateAccessor(HttpContext? context) =>
        new(new HttpContextAccessor { HttpContext = context });
}
