namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Configures the limits per client address. The strict limit protects the anonymous invite endpoints.
/// The default limit applies to every other route that the gateway proxies.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimit Strict { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public FixedWindowLimit Default { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };
}
