using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Spa.Web.IndexDocument;

/// <summary>
/// Reports healthy when the index document is loaded. The check resolves the singleton, so a missing file shows
/// as a failed check.
/// </summary>
public sealed class IndexDocumentHealthCheck(IndexDocument indexDocument) : IHealthCheck
{
    private readonly IndexDocument _indexDocument = indexDocument;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => Task.FromResult(HealthCheckResult.Healthy());
}
