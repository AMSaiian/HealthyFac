using HealthyFac.Shared.Web.CrossOrigin;

namespace HealthyFac.Shared.UnitTests.CrossOrigin.Data;

public static class CorsPolicyOptionsValidatorTestData
{
    public static TheoryData<int> AboveBrowserLimit => new()
    {
        7201,
    };

    public static TheoryData<int> ValidPreflightMaxAges => new()
    {
        1,
        7200,
    };

    public static string AboveBrowserLimitMessage { get; } = CorsValidationMessages.PreflightMaxAgeAboveBrowserLimit
        .Replace(oldValue: "{ComparisonValue}", CorsConstants.MaxPreflightMaxAgeSeconds.ToString());

    public static CorsPolicyOptions EnabledWithPreflightMaxAge(int seconds) =>
        new()
        {
            IsEnabled = true,
            AllowedOrigins = ["http://app.healthyfac.test:4200"],
            PreflightMaxAgeSeconds = seconds,
        };
}
