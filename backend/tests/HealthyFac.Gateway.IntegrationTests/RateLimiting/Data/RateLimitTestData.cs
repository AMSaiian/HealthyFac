namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class RateLimitTestData
{
    public const int StrictPermitLimit = 3;
    public const int DefaultPermitLimit = 2;
    public const int BlobPermitLimit = 3;
    public const string BlobRoute = "/api/document/blobs/files/00000000-0000-0000-0000-000000000001?sp=r";
    public const int DefaultRequestsDuringOutage = 20;
    public const int StaticRequestCount = 150;
    public const int DefaultApiPermitLimit = 300;
    public const string SpaRoute = "http://app.healthyfac.test/machines";
    public const string VerifyInviteRoute = "/api/core/v1/employee-invites/verify";
    public const string MachinesRoute = "/api/core/v1/machines";
    public const string ForwardedForHeader = "X-Forwarded-For";
    public const string ProblemJsonMediaType = "application/problem+json";

    public static IReadOnlyDictionary<string, string> DefaultLimitOfTwo { get; } = new Dictionary<string, string>
    {
        [key: "RateLimiting:Default:PermitLimit"] = DefaultPermitLimit.ToString(),
        [key: "RateLimiting:Default:WindowSeconds"] = "60",
    };

    public static IReadOnlyDictionary<string, string> BlobLimitOfThree { get; } = new Dictionary<string, string>
    {
        [key: "RateLimiting:BlobDownload:PermitLimit"] = BlobPermitLimit.ToString(),
        [key: "RateLimiting:BlobDownload:WindowSeconds"] = "60",
    };

    public static IReadOnlyDictionary<string, string> StrictLimitOfThree { get; } = new Dictionary<string, string>
    {
        [key: "RateLimiting:Strict:PermitLimit"] = StrictPermitLimit.ToString(),
        [key: "RateLimiting:Strict:WindowSeconds"] = "60",
    };
}
