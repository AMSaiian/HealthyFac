namespace HealthyFac.Gateway.Web.RateLimiting;

public sealed class FixedWindowLimit
{
    public required int PermitLimit { get; init; }

    public required int WindowSeconds { get; init; }
}
