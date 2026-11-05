using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class IncomingTokenTestData
{
    public const string UserAccessToken = "user-access-token";
    public const string ServiceAccessToken = "service-access-token";
    public const string ServiceClientId = "healthyfac-auth";
    public const string ClientIdClaim = "client_id";
    public const string BearerScheme = "Bearer";
    public const string LowerCaseBearerScheme = "bearer";

    public static readonly Guid ServiceAccountId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000b1");

    public static ClaimsPrincipal ServicePrincipal => new(
        new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, ServiceAccountId.ToString()),
                new Claim(ClientIdClaim, ServiceClientId),
            ],
            authenticationType: "test"));

    public static DefaultHttpContext CreateContext(
        ClaimsPrincipal user,
        string accessToken,
        string scheme = BearerScheme
    )
    {
        var context = new DefaultHttpContext { User = user };
        context.Request.Headers.Authorization = $"{scheme} {accessToken}";

        return context;
    }
}
