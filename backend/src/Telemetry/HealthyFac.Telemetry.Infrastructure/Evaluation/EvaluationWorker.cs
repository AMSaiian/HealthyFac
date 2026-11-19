using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation;

/// <summary>
/// The background loop that evaluates the machines. A tick checks whether the whole fleet is silent, then claims and
/// runs the due machines one at a time until none is due.
/// </summary>
/// <remarks>
/// Each unit (the fleet check, a claim, a run) has its own DI scope and actor. The loop checks the stop token between
/// machines only: a run in progress sees the token inside the use case, which stores nothing when it is cancelled
/// before its save. A failed run is logged as an error, and its lease expires, so a later round takes the machine
/// again. Each run is a use case, so it has its own trace.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of each unit.</param>
/// <param name="options">Holds the interval and the switch of the worker.</param>
/// <param name="timeProvider">Provides the clock of the tick.</param>
/// <param name="fleetSilence">Receives the result of the fleet check.</param>
/// <param name="metrics">Records the runs and the due machines.</param>
/// <param name="logger">Logs the failures of a run.</param>
public sealed class EvaluationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DetectionOptions> options,
    TimeProvider timeProvider,
    FleetSilence fleetSilence,
    DetectionMetrics metrics,
    ILogger<EvaluationWorker> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly DetectionOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly FleetSilence _fleetSilence = fleetSilence;
    private readonly DetectionMetrics _metrics = metrics;
    private readonly ILogger<EvaluationWorker> _logger = logger;

    /// <summary>
    /// Runs one round: the fleet check, then every due machine.
    /// </summary>
    /// <param name="cancellationToken">The token that stops the service.</param>
    /// <returns>The number of machines that the round ran.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await CheckFleetAsync(cancellationToken);
        await SampleDueMachinesAsync(cancellationToken);
        var runCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimed = await ClaimAsync(cancellationToken);

            if (claimed is null)
            {
                return runCount;
            }

            await RunAsync(claimed, cancellationToken);
            runCount++;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsWorkerEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.EvaluationIntervalSeconds), _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunRoundAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunRoundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunOnceAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, message: "The evaluation round failed. The loop goes on.");
        }
    }

    private async Task CheckFleetAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Evaluator);
        var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

        await _fleetSilence.CheckAsync(dbContext, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    private async Task SampleDueMachinesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Evaluator);
        var claims = scope.ServiceProvider.GetRequiredService<EvaluationClaims>();

        _metrics.UpdateDueMachines(await claims.CountDueAsync(cancellationToken));
    }

    private async Task<ClaimedEvaluation?> ClaimAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Evaluator);
        var claims = scope.ServiceProvider.GetRequiredService<EvaluationClaims>();

        return await claims.ClaimNextAsync(cancellationToken);
    }

    private async Task RunAsync(ClaimedEvaluation claimed, CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();

        try
        {
            using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Evaluator);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<EvaluateMachineRequest>>();

            await handler.HandleAsync(
                new EvaluateMachineRequest { MachineId = claimed.MachineId, CompanyId = claimed.CompanyId },
                cancellationToken);
            _metrics.RecordRun(isCompleted: true, _timeProvider.GetElapsedTime(startedAt));
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _metrics.RecordRun(isCompleted: false, _timeProvider.GetElapsedTime(startedAt));
            _logger.LogError(
                exception,
                message: "The evaluation of machine {MachineId} failed. Its lease expires and a later round retries.",
                claimed.MachineId);
        }
    }
}
