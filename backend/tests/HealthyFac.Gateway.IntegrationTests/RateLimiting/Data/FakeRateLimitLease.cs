using System.Threading.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public sealed class FakeRateLimitLease(bool isAcquired) : RateLimitLease
{
    public override bool IsAcquired => isAcquired;

    public override IEnumerable<string> MetadataNames => [];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;

        return false;
    }
}
