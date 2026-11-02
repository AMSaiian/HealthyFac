using System.Threading.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public sealed class FakeRateLimitLease(bool isAcquired) : RateLimitLease
{
    private readonly bool _isAcquired = isAcquired;

    public override bool IsAcquired => _isAcquired;

    public override IEnumerable<string> MetadataNames => [];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;

        return false;
    }
}
