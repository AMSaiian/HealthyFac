using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Samples the pending outbox and inbox rows every <see cref="MessagingConstants.BacklogSampleInterval"/> and
/// stores the result in <see cref="MessagingMetrics"/>. The gauge callbacks read only the stored sample, so the
/// metric export never waits for the database. Each table needs one query on its filtered pending index. The sampler
/// runs without a parent span, so its queries create no trace. Every instance reports the same table values, so a
/// dashboard aggregates the gauges with <c>max</c>, never <c>sum</c>.
/// </summary>
/// <param name="scopeFactory">Creates the scope of each sample.</param>
/// <param name="metrics">Stores the sample for the gauges.</param>
/// <param name="timeProvider">The clock of the sample interval.</param>
/// <param name="logger">The logger of the sampler.</param>
internal sealed class MessageBacklogSampler(
    IServiceScopeFactory scopeFactory,
    MessagingMetrics metrics,
    TimeProvider timeProvider,
    ILogger<MessageBacklogSampler> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly MessagingMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<MessageBacklogSampler> _logger = logger;

    /// <summary>
    /// Reads the pending count and the oldest pending time of both tables and stores them. An inbox row in the
    /// failed state is not pending: no processor claims it again.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the sample is stored.</returns>
    public async Task SampleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();
        var inboxContext = scope.ServiceProvider.GetRequiredService<IInboxDbContext>();

        var outbox = await outboxContext.OutboxMessages
            .Where(message => message.ProcessedOn == null)
            .GroupBy(_ => 1)
            .Select(pending => new PendingBacklog
            {
                Count = pending.Count(),
                OldestOn = pending.Min(message => (DateTime?)message.OccurredOn),
            })
            .SingleOrDefaultAsync(cancellationToken);
        var inbox = await inboxContext.InboxMessages
            .Where(message => message.ProcessedOn == null && message.FailedOn == null)
            .GroupBy(_ => 1)
            .Select(pending => new PendingBacklog
            {
                Count = pending.Count(),
                OldestOn = pending.Min(message => (DateTime?)message.ReceivedOn),
            })
            .SingleOrDefaultAsync(cancellationToken);

        _metrics.UpdateBacklog(
            new MessageBacklog
            {
                Outbox = outbox ?? PendingBacklog.Empty,
                Inbox = inbox ?? PendingBacklog.Empty,
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SampleSafelyAsync(stoppingToken);
            await Task.Delay(MessagingConstants.BacklogSampleInterval, _timeProvider, stoppingToken);
        }
    }

    /// <summary>
    /// Runs one sample. A failure clears the stored sample, so the gauges report no stale backlog while the database
    /// is down.
    /// </summary>
    /// <param name="cancellationToken">The token of the host.</param>
    /// <returns>A task that completes when the sample ends.</returns>
    private async Task SampleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SampleAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _metrics.UpdateBacklog(null);
            _logger.LogWarning(
                exception, "The message backlog sample failed. The next sample runs after the interval.");
        }
    }
}
