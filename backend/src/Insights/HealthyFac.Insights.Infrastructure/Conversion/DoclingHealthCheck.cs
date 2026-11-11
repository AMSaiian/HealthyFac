using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Insights.Infrastructure.Conversion;

/// <summary>
/// Checks that docling-serve answers its health path. Insights still takes uploads and queues attempts while Docling
/// is down, so a failure reports <see cref="HealthStatus.Degraded"/> and never <see cref="HealthStatus.Unhealthy"/>,
/// which would take the service out of the load balancer.
/// </summary>
/// <remarks>
/// The check reuses one client of the named Docling client for its lifetime and gives each call its own limit of
/// <see cref="Timeout"/>, because the client itself has no time limit.
/// </remarks>
/// <param name="httpClientFactory">Creates the Docling client once.</param>
public sealed class DoclingHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string Name = "docling";

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(seconds: 2);

    private const string UnhealthyStatusFormat = "Docling answered with the status {0}.";
    private const string UnreachableMessage = "Docling did not answer.";

    private readonly Lazy<HttpClient> _client = new(
        () => httpClientFactory.CreateClient(DoclingConstants.ClientName));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Timeout);

        try
        {
            using var response = await _client.Value.GetAsync(DoclingConstants.HealthPath, limit.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded(string.Format(UnhealthyStatusFormat, (int)response.StatusCode));
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(UnreachableMessage, exception);
        }
    }
}

/// <summary>
/// Provides the extension method that registers the Docling check.
/// </summary>
public static class DoclingHealthCheckExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// Registers <see cref="DoclingHealthCheck"/> with the failure status <see cref="HealthStatus.Degraded"/>.
        /// </summary>
        /// <param name="tags">The tags of the check.</param>
        /// <returns>The updated <see cref="IHealthChecksBuilder"/> instance for method chaining.</returns>
        public IHealthChecksBuilder AddDoclingHealthCheck(IEnumerable<string> tags) =>
            builder.AddCheck<DoclingHealthCheck>(DoclingHealthCheck.Name, HealthStatus.Degraded, tags);
    }
}
