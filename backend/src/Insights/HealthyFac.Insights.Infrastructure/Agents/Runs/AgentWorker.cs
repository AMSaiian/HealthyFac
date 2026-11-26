using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Agents.Runs;

/// <summary>
/// The background loop that runs the agent runs: it sweeps the runs whose lease ended, claims the oldest due run, and
/// runs it as the advisor actor of the company of the run. It runs one run at a time.
/// </summary>
/// <remarks>
/// Each unit (a sweep, a claim, a run) has its own DI scope and actor. The loop checks the stop token between units
/// only. A failure of a unit is logged, and the loop goes on after a delay. A run that a stop interrupts keeps its
/// lease, so the sweep of the next start replaces it.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of each unit.</param>
/// <param name="options">Holds the poll delay and the switch of the worker.</param>
/// <param name="timeProvider">Provides the clock of the delay.</param>
/// <param name="metrics">Records the finished runs and the queue.</param>
/// <param name="logger">Logs the failures of the loop.</param>
public sealed class AgentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AgentOptions> options,
    TimeProvider timeProvider,
    AgentMetrics metrics,
    ILogger<AgentWorker> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AgentOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly AgentMetrics _metrics = metrics;
    private readonly ILogger<AgentWorker> _logger = logger;

    /// <summary>
    /// Runs one round: it sweeps every expired lease, then claims and runs at most one run.
    /// </summary>
    /// <param name="cancellationToken">The token that stops the service.</param>
    /// <returns><see langword="true"/> when the round ran a run; <see langword="false"/> when none was due.</returns>
    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        while (await SweepOneAsync(cancellationToken) is { } interruptedRole)
        {
            _metrics.RecordInterrupted(interruptedRole);
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsWorkerEnabled)
        {
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(_options.PollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var didRun = await RunOnceAsync(stoppingToken);

                if (!didRun)
                {
                    await Task.Delay(pollInterval, _timeProvider, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, message: "The agent worker round failed. The loop goes on.");
                await DelayAfterFailureAsync(pollInterval, stoppingToken);
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

    private async Task<AgentRole?> SweepOneAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(AgentServiceActors.Worker);
        var claims = scope.ServiceProvider.GetRequiredService<AgentRunClaims>();

        return await claims.InterruptNextExpiredAsync(cancellationToken);
    }

    private async Task<ClaimedRun?> ClaimAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(AgentServiceActors.Worker);
        var claims = scope.ServiceProvider.GetRequiredService<AgentRunClaims>();

        var claimed = await claims.ClaimNextAsync(cancellationToken);
        _metrics.UpdateQueuedRuns(await claims.CountDueAsync(cancellationToken));

        return claimed;
    }

    private async Task RunAsync(ClaimedRun claimed, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(AgentServiceActors.Advisor(claimed.CompanyId));
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<RunAgentRequest>>();

        await handler.HandleAsync(
            new RunAgentRequest { RunId = claimed.RunId, CompanyId = claimed.CompanyId },
            cancellationToken);

        var claims = scope.ServiceProvider.GetRequiredService<AgentRunClaims>();
        var run = await claims.LoadAsync(claimed.RunId, cancellationToken);
        RecordFinishedRun(claimed.Role, run);
    }

    private void RecordFinishedRun(AgentRole role, AgentRun? run)
    {
        if (run?.StartedAt is not { } startedAt || run.FinishedAt is not { } finishedAt)
        {
            return;
        }

        if (run.State == AgentRunState.Completed)
        {
            _metrics.RecordRun(role, ObservabilityConstants.Agents.Results.Completed, finishedAt - startedAt);
        }
        else if (run.State == AgentRunState.Failed)
        {
            _metrics.RecordRun(role, ObservabilityConstants.Agents.Results.Failed, finishedAt - startedAt);
        }
    }
}
