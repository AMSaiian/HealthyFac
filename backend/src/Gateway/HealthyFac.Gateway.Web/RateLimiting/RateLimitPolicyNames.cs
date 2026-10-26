namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Defines the rate limiter policy names. The names also appear as <c>RateLimiterPolicy</c> values in the
/// reverse proxy route configuration. YARP reserves the route policy names "default" and "disable", so the
/// default policy needs another name.
/// </summary>
public static class RateLimitPolicyNames
{
    public const string AnonymousStrict = "anonymous-strict";
    public const string Default = "default-limit";
}
