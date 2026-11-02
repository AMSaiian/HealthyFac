using System.Threading.RateLimiting;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// A lease that grants nothing and consumes nothing. A synchronous acquire returns it, so the caller continues
/// with the asynchronous acquire, which decides.
/// </summary>
internal sealed class DeniedRateLimitLease : RateLimitLease
{
    public static DeniedRateLimitLease Instance { get; } = new();

    public override bool IsAcquired => false;

    public override IEnumerable<string> MetadataNames => [];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;

        return false;
    }
}
