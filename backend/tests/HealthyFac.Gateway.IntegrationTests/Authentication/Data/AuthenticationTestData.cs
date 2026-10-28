namespace HealthyFac.Gateway.IntegrationTests.Authentication.Data;

public static class AuthenticationTestData
{
    public const string CoreRoute = "/api/core/v1/machines";
    public const string VerifyInviteRoute = "/api/core/v1/employee-invites/verify";
    public const string AdminRoute = "/admin/master/console/";
    public const string AuthorizationHeader = "Authorization";
    public const string OtherAudience = "healthyfac-other-api";
}
