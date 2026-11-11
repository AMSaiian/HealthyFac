using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Domain.Ingest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Samples the queued attempts every 15 seconds and stores the result in <see cref="IngestMetrics"/>. The gauge
/// callbacks read only the stored sample, so the metric export never waits for the database. One query on the
/// <c>(state, queued_at)</c> index reads the count and the oldest time. The sampler runs without a parent span, so its
/// query creates no trace. Every instance reports the same value, so a dashboard aggregates the gauges with
/// <c>max</c>, never <c>sum</c>.
/// </summary>
/// <param name="scopeFactory">Creates the scope of each sample.</param>
/// <param name="metrics">Stores the sample for the gauges.</param>
/// <param name="timeProvider">The clock of the sample interval.</param>
/// <param name="logger">The logger of the sampler.</param>
public sealed class IngestBacklogSampler(
    IServiceScopeFactory scopeFactory,
    IngestMetrics metrics,
    TimeProvider timeProvider,
    ILogger<IngestBacklogSampler> logger
) : BackgroundService
{
    public static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(seconds: 15);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IngestMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IngestBacklogSampler> _logger = logger;

    /// <summary>
    /// Reads the number of queued attempts and the time of the oldest one, and stores them.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>A task that completes when the sample is stored.</returns>
    public async Task SampleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

        var queued = await dbContext.IngestAttempts
            .AsNoTracking()
            .Where(attempt => attempt.State == IngestState.Queued)
            .GroupBy(_ => 1)
            .Select(pending => new IngestBacklog
            {
                Count = pending.Count(),
                OldestQueuedAt = pending.Min(attempt => (DateTime?)attempt.QueuedAt),
            })
            .SingleOrDefaultAsync(cancellationToken);

        _metrics.UpdateBacklog(queued ?? IngestBacklog.Empty);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await SampleSafelyAsync(cancellationToken);
            await Task.Delay(SampleInterval, _timeProvider, cancellationToken);
        }
    }

    /// <summary>
    /// Runs one sample. A failure clears the stored sample, so the gauges report no stale queue while the database is
    /// down.
    /// </summary>
    /// <param name="cancellationToken">The token of the host.</param>
    /// <returns>A task that ends with the sample.</returns>
    private async Task SampleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SampleAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _metrics.UpdateBacklog(backlog: null);
            _logger.LogWarning(exception, message: "The ingest queue sample failed. The gauges report no value.");
        }
    }
}
