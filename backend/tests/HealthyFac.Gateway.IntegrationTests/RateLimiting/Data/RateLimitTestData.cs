namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class RateLimitTestData
{
    public const int StrictPermitLimit = 3;
    public const int DefaultRequestsDuringOutage = 20;
    public const string VerifyInviteRoute = "/api/core/v1/employee-invites/verify";
    public const string MachinesRoute = "/api/core/v1/machines";
    public const string ForwardedForHeader = "X-Forwarded-For";
    public const string ProblemJsonMediaType = "application/problem+json";

    public static IReadOnlyDictionary<string, string> StrictLimitOfThree { get; } = new Dictionary<string, string>
    {
        ["RateLimiting:Strict:PermitLimit"] = StrictPermitLimit.ToString(),
        ["RateLimiting:Strict:WindowSeconds"] = "60",
    };
}
