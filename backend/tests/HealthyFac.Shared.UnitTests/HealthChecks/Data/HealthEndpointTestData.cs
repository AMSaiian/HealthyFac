using HealthyFac.Shared.Web.HealthChecks;

namespace HealthyFac.Shared.UnitTests.HealthChecks.Data;

public static class HealthEndpointTestData
{
    public static TheoryData<string, string> PublicPortRoutes => new()
    {
        { "readiness", HealthCheckConstants.Routes.Readiness },
        { "detailed", HealthCheckConstants.Routes.Detailed },
    };
}
