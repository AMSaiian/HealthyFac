using HealthyFac.Shared.Web.CrossOrigin;

namespace HealthyFac.Shared.UnitTests.CrossOrigin.Data;

public static class CorsPolicyOptionsValidatorTestData
{
    public static TheoryData<int> AboveBrowserLimit => new()
    {
        7201,
        int.MaxValue,
    };

    public static TheoryData<int> ValidPreflightMaxAges => new()
    {
        1,
        600,
        7200,
    };

    public static string AboveBrowserLimitMessage { get; } = CorsValidationMessages.PreflightMaxAgeAboveBrowserLimit
        .Replace("{ComparisonValue}", CorsConstants.MaxPreflightMaxAgeSeconds.ToString());

    public static CorsPolicyOptions EnabledWithPreflightMaxAge(int seconds) =>
        new()
        {
            Enabled = true,
            AllowedOrigins = ["http://app.healthyfac.test:4200"],
            PreflightMaxAgeSeconds = seconds,
        };
}
