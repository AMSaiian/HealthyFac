using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Reports whether the blob storage answers. The service works without it for reads of the file grids, so a failure
/// gives <see cref="HealthStatus.Degraded"/> and never <see cref="HealthStatus.Unhealthy"/>. The check uses a linked
/// timeout, so a slow storage never holds the readiness probe.
/// </summary>
/// <param name="storage">The storage whose container the check reads.</param>
public sealed class BlobStorageHealthCheck(BlobFileStorage storage) : IHealthCheck
{
    private readonly BlobFileStorage _storage = storage;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(FileStorageConstants.HealthTimeoutSeconds));

        try
        {
            await _storage.CheckContainerAsync(timeout.Token);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(FileStorageErrors.Unavailable, exception);
        }
    }
}
