using HealthyFac.Gateway.Web.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class RateLimitOptionsValidatorTestData
{
    private static readonly FixedWindowLimit ValidLimit = new() { PermitLimit = 5, WindowSeconds = 30 };
    private static readonly FixedWindowLimit ZeroPermitLimit = new() { PermitLimit = 0, WindowSeconds = 30 };
    private static readonly FixedWindowLimit NegativeWindowLimit = new() { PermitLimit = 5, WindowSeconds = -1 };

    public static TheoryData<RateLimitOptions, string> InvalidOptionsWithExpectedError => new()
    {
        {
            new RateLimitOptions { Strict = ZeroPermitLimit, Default = ValidLimit },
            RateLimitValidationMessages.Strict.PermitLimitTooSmall
        },
        {
            new RateLimitOptions { Strict = NegativeWindowLimit, Default = ValidLimit },
            RateLimitValidationMessages.Strict.WindowTooSmall
        },
        {
            new RateLimitOptions { Strict = ValidLimit, Default = ZeroPermitLimit },
            RateLimitValidationMessages.Default.PermitLimitTooSmall
        },
        {
            new RateLimitOptions { Strict = ValidLimit, Default = NegativeWindowLimit },
            RateLimitValidationMessages.Default.WindowTooSmall
        },
    };
}
