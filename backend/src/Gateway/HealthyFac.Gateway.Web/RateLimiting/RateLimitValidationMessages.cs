namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Defines the validator messages for the rate limit configuration, grouped by the policy they report.
/// </summary>
public static class RateLimitValidationMessages
{
    public static class Strict
    {
        public const string PermitLimitTooSmall = "The strict rate limit must allow at least 1 request.";
        public const string WindowTooSmall = "The strict rate limit window must be at least 1 second.";
    }

    public static class Default
    {
        public const string PermitLimitTooSmall = "The default rate limit must allow at least 1 request.";
        public const string WindowTooSmall = "The default rate limit window must be at least 1 second.";
    }
}
