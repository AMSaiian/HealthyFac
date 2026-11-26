using System.Diagnostics;
using HealthyFac.MachineSimulator.Machines.Snapshots;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Options;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// Runs the three loops of the simulator: the publish loop, the state poll, and the discovery. Each loop runs on its
/// own <see cref="PeriodicTimer"/> and stops with the host.
/// </summary>
/// <param name="engine">Holds the machines.</param>
/// <param name="publisher">Sends the messages to the broker.</param>
/// <param name="metrics">Counts the messages and the polls.</param>
/// <param name="options">The intervals of the loops.</param>
/// <param name="scopeFactory">Opens a scope for the readers, which use scoped database contexts.</param>
/// <param name="timeProvider">Times the loops and gives the UTC instants.</param>
/// <param name="logger">Logs the messages at debug level and the failed polls.</param>
public sealed class SimulationLoops(
    SimulationEngine engine,
    IMachineMessagePublisher publisher,
    SimulatorMetrics metrics,
    IOptions<SimulatorOptions> options,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<SimulationLoops> logger
) : BackgroundService
{
    private static readonly ActivitySource Source = new(ObservabilityConstants.Simulator.Source);

    private readonly SimulationEngine _engine = engine;
    private readonly IMachineMessagePublisher _publisher = publisher;
    private readonly SimulatorMetrics _metrics = metrics;
    private readonly SimulatorOptions _options = options.Value;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<SimulationLoops> _logger = logger;

    /// <summary>
    /// Makes one tick and publishes its messages. The loop checks the token before each tick, never inside one. A
    /// dropped message is not tried again, and the counters of the machine have grown already.
    /// </summary>
    /// <param name="cancellationToken">Cancels the publishes.</param>
    /// <returns>A task that ends after the last publish.</returns>
    public async Task PublishTickAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var messages = _engine.BuildTick(_timeProvider.GetUtcNow().UtcDateTime);

        foreach (var message in messages)
        {
            var isPublished = await _publisher.TryPublishAsync(message.GatewayId, message.Payload, cancellationToken);
            _metrics.RecordMessage(message.Scenario, isPublished);

            _logger.LogDebug(
                message: "{Outcome} a message of the gateway {GatewayId} with the keys {Keys} and the codes {Codes}",
                isPublished ? "Published" : "Dropped",
                message.GatewayId,
                message.Keys,
                message.Codes);
        }
    }

    /// <summary>
    /// Reads the machines and applies them to the known ones. A removed machine leaves the engine.
    /// </summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>A task that ends after the apply.</returns>
    public async Task PollStateAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<MachineSnapshotReader>();
        var snapshots = await reader.ReadAllAsync(cancellationToken);

        _engine.ApplyStatePoll(snapshots, _timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>
    /// Reads the machines, reads the stored counters of the new ones, and applies both.
    /// </summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>A task that ends after the apply.</returns>
    public async Task DiscoverAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<MachineSnapshotReader>();
        var counterReader = scope.ServiceProvider.GetRequiredService<CounterReadingReader>();
        var snapshots = await reader.ReadAllAsync(cancellationToken);
        var known = _engine.KnownMachineIds().ToHashSet();
        Dictionary<Guid, IReadOnlyDictionary<string, double>> counterStarts = [];

        foreach (var snapshot in snapshots.Where(snapshot => !known.Contains(snapshot.MachineId)))
        {
            var keys = ScenarioBinding.Create(ScenarioBinding.KindOf(snapshot.SerialNumber)).CounterStartValues.Keys;

            if (keys.Any())
            {
                counterStarts[snapshot.MachineId] = await counterReader.ReadLatestAsync(
                    snapshot.MachineId,
                    [.. keys],
                    cancellationToken);
            }
        }

        _engine.ApplyDiscovery(snapshots, counterStarts, _timeProvider.GetUtcNow().UtcDateTime);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _metrics.SetPublishingSource(() => _engine.PublishingCount(_timeProvider.GetUtcNow().UtcDateTime));

        try
        {
            await Task.WhenAll(
                RunLoopAsync(_options.PublishInterval, PublishTickAsync, stoppingToken),
                RunPollLoopAsync(
                    _options.StatePollInterval,
                    ObservabilityConstants.Simulator.Spans.PollState,
                    ObservabilityConstants.Simulator.Polls.State,
                    PollStateAsync,
                    stoppingToken),
                RunPollLoopAsync(
                    _options.DiscoveryInterval,
                    ObservabilityConstants.Simulator.Spans.Discover,
                    ObservabilityConstants.Simulator.Polls.Discovery,
                    DiscoverAsync,
                    stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunLoopAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken
    )
    {
        using var timer = new PeriodicTimer(interval, _timeProvider);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await work(cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(exception, message: "The publish tick failed; the next tick runs as usual");
            }
        }
    }

    private async Task RunPollLoopAsync(
        TimeSpan interval,
        string spanName,
        string pollName,
        Func<CancellationToken, Task> poll,
        CancellationToken cancellationToken
    )
    {
        using var timer = new PeriodicTimer(interval, _timeProvider);

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RunPollAsync(spanName, pollName, poll, cancellationToken);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private async Task RunPollAsync(
        string spanName,
        string pollName,
        Func<CancellationToken, Task> poll,
        CancellationToken cancellationToken
    )
    {
        using var activity = Source.StartActivity(spanName);
        var startedAt = _timeProvider.GetTimestamp();

        try
        {
            await poll(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception, message: "The {Poll} poll failed; the machines keep their last state", pollName);

            activity?.SetStatus(ActivityStatusCode.Error);
        }

        _metrics.RecordPoll(pollName, _timeProvider.GetElapsedTime(startedAt));
    }
}
