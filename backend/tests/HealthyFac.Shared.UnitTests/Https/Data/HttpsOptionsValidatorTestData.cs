using HealthyFac.Shared.Web.Https;

namespace HealthyFac.Shared.UnitTests.Https.Data;

public static class HttpsOptionsValidatorTestData
{
    public static HttpsOptions PreloadAtMinimumMaxAge => CreatePreloadOptions(HttpsConstants.PreloadMinimumMaxAgeDays);

    public static HttpsOptions PreloadBelowMinimumMaxAge =>
        CreatePreloadOptions(HttpsConstants.PreloadMinimumMaxAgeDays - 1);

    private static HttpsOptions CreatePreloadOptions(int maxAgeDays) =>
        new()
        {
            IsHstsEnabled = true,
            IsHstsSubDomainsIncluded = true,
            IsHstsPreloaded = true,
            HstsMaxAgeDays = maxAgeDays,
        };
}
