using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// The background loop that runs the ingest attempts: it sweeps the attempts whose lease ended, claims the oldest
/// queued attempt, and runs it as the user who queued it. It runs one attempt at a time.
/// </summary>
/// <remarks>
/// Each unit (a sweep, a claim, a run) has its own DI scope and actor. The loop checks the stop token between units
/// only: a run in progress sees the token inside the use case, which interrupts its attempt with the reason
/// <c>serviceStopped</c>. A failure of a unit is logged, and the loop goes on after a delay.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of each unit.</param>
/// <param name="options">Holds the poll delay and the switch of the worker.</param>
/// <param name="timeProvider">Provides the clock of the delay.</param>
/// <param name="logger">Logs the failures of the loop.</param>
public sealed class IngestWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<IngestWorker> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IngestProcessingOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IngestWorker> _logger = logger;

    /// <summary>
    /// Runs one round: it sweeps every expired lease, then claims and runs at most one attempt.
    /// </summary>
    /// <param name="cancellationToken">The token that stops the service.</param>
    /// <returns>
    /// <see langword="true"/> when the round ran an attempt; <see langword="false"/> when none was queued.
    /// </returns>
    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        while (await SweepOneAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var claimed = await ClaimAsync(cancellationToken);
        if (claimed is null)
        {
            return false;
        }

        await RunAsync(claimed, cancellationToken);

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsWorkerEnabled)
        {
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(_options.PollSeconds);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var didRun = await RunOnceAsync(cancellationToken);
                if (!didRun)
                {
                    await Task.Delay(pollInterval, _timeProvider, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, message: "The ingest worker round failed. The loop goes on.");
                await DelayAfterFailureAsync(pollInterval, cancellationToken);
            }
        }
    }

    private async Task DelayAfterFailureAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, _timeProvider, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task<bool> SweepOneAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(IngestServiceActors.Worker);
        var claims = scope.ServiceProvider.GetRequiredService<IngestAttemptClaims>();

        return await claims.InterruptNextExpiredAsync(cancellationToken);
    }

    private async Task<ClaimedAttempt?> ClaimAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(IngestServiceActors.Worker);
        var claims = scope.ServiceProvider.GetRequiredService<IngestAttemptClaims>();

        return await claims.ClaimNextAsync(cancellationToken);
    }

    private async Task RunAsync(ClaimedAttempt claimed, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(KnownActors.Restore(claimed.QueuedBy, claimed.CompanyId));
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<ProcessIngestAttemptRequest>>();

        await handler.HandleAsync(
            new ProcessIngestAttemptRequest { AttemptId = claimed.AttemptId, CompanyId = claimed.CompanyId },
            cancellationToken);
    }
}
