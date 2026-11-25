using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.TelemetryHistory.Persistence;
using HealthyFac.Seeder.TelemetryHistory.Simulation;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// The state of one run of the history between its steps: the simulation and the message builder of each machine, the
/// end of the last step, and the anchor sample of the last machine for the marker. A step writes the new samples of
/// all machines in one call and then runs the evaluation, as the live evaluator does after the intake.
/// </summary>
internal sealed class HistoryRun
{
    private readonly ServiceHost _telemetry;
    private readonly IReadOnlyList<MachineRun> _machines;
    private readonly DateTimeOffset _anchor;
    private readonly DateTime _windowStart;
    private readonly ReadingRowWriter _writer = new();
    private DateTime _lastTo;
    private HistorySample? _anchorSample;

    private HistoryRun(
        ServiceHost telemetry,
        IReadOnlyList<MachineRun> machines,
        DateTimeOffset anchor,
        int windowHours
    )
    {
        _telemetry = telemetry;
        _machines = machines;
        _anchor = anchor;
        _windowStart = anchor.UtcDateTime.AddHours(-windowHours);
        _lastTo = _windowStart;
    }

    /// <summary>Reads the machines from Telemetry and builds a simulation for each one.</summary>
    /// <param name="telemetry">The Telemetry host.</param>
    /// <param name="file">The telemetry script.</param>
    /// <param name="machinesFile">The machines and their state changes.</param>
    /// <param name="machines">The machines of <c>machines.json</c> with their drafts.</param>
    /// <param name="context">The IDs of the machines, the anchor, and the company zone.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The run.</returns>
    public static async Task<HistoryRun> CreateAsync(
        ServiceHost telemetry,
        TelemetryFile file,
        MachinesFile machinesFile,
        IReadOnlyList<HistoryMachine> machines,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var windowStart = context.Anchor.UtcDateTime.AddHours(-file.WindowHours);
        var runs = new List<MachineRun>();
        foreach (var machine in machines.OrderBy(candidate => candidate.Index))
        {
            var machineId = context.GetId($"{machine.Key}{MachinesConstants.MachineSuffix}");
            var data = await ReadDataAsync(telemetry, machineId, cancellationToken);
            var changes = ResolveChanges(machine.Key, file, machinesFile, context, windowStart);
            var snapshot = new MachineSnapshot
            {
                MachineId = machineId,
                CompanyId = data.CompanyId,
                Name = machine.Draft.Name!,
                SerialNumber = machine.Draft.SerialNumber!,
                GatewayId = data.GatewayId,
                ExpectedGoLiveOn = data.ExpectedGoLiveOn,
                Zone = context.Zone,
                StateHistory =
                [
                    new StateHistoryEntry
                    {
                        State = MachineState.Operational,
                        StartedAt = windowStart - TelemetryHistoryConstants.InitialStateLead,
                    },
                    .. changes.Where(change => change.StartedAt <= windowStart),
                ],
                IsRegistered = true,
                Definitions = data.Definitions,
            };
            var events = new HistoryEvents(
                file.Drains.Where(drain => drain.Machine == machine.Key),
                file.Steps.Where(step => step.Machine == machine.Key),
                file.Gaps.Where(gap => gap.Machine == machine.Key),
                file.Overrides.Where(overrideSeed => overrideSeed.Machine == machine.Key),
                context.Anchor,
                context.Zone);
            var simulation = new HistorySimulation(
                snapshot,
                windowStart,
                TimeSpan.FromSeconds(file.SampleIntervalSeconds),
                new Random(file.RandomSeed + machine.Index),
                changes.Where(change => change.StartedAt > windowStart).ToList(),
                events);

            runs.Add(new MachineRun { Key = machine.Key, MachineId = machineId, Data = data, Simulation = simulation });
        }

        return new HistoryRun(telemetry, runs, context.Anchor, file.WindowHours);
    }

    /// <summary>Writes the samples up to a time and evaluates the machines.</summary>
    /// <param name="at">The UTC time of the step.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes after the evaluation.</returns>
    public async Task RunStepAsync(DateTime at, CancellationToken cancellationToken)
    {
        if (at < _windowStart)
        {
            return;
        }

        var batch = new List<HistoryRows>();
        foreach (var machine in _machines)
        {
            var samples = machine.Simulation.Run(_lastTo, at);
            var isLast = machine == _machines[^1];
            foreach (var sample in samples)
            {
                if (isLast && sample.SampledAt == _anchor.UtcDateTime)
                {
                    _anchorSample = sample;

                    continue;
                }

                batch.Add(machine.Builder(_telemetry).Build(sample));
            }
        }

        _lastTo = at;
        using (var scope = _telemetry.CreateScope(TelemetryServiceActors.Evaluator))
        {
            await _writer.WriteAsync(
                scope.ServiceProvider.GetRequiredService<TelemetryDbContext>(), batch, cancellationToken);
            await scope.ServiceProvider.GetRequiredService<FleetSilence>()
                .CheckAsync(scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>(), at, cancellationToken);
        }

        foreach (var machine in _machines)
        {
            await _telemetry.SendAsync(
                TelemetryServiceActors.Evaluator,
                new EvaluateMachineRequest { MachineId = machine.MachineId, CompanyId = machine.Data.CompanyId },
                cancellationToken);
        }
    }

    /// <summary>Writes the anchor sample of the last machine with the ID of the marker.</summary>
    /// <param name="markerMessageId">The ID of the marker message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the marker is stored.</returns>
    /// <exception cref="InvalidOperationException">The last machine has no sample at the anchor.</exception>
    public async Task WriteMarkerAsync(Guid markerMessageId, CancellationToken cancellationToken)
    {
        var last = _machines[^1];
        var sample = _anchorSample
            ?? throw new InvalidOperationException(TelemetryHistoryErrors.AnchorSampleMissing(last.Key));

        using var scope = _telemetry.CreateScope(TelemetryServiceActors.Evaluator);
        await _writer.WriteAsync(
            scope.ServiceProvider.GetRequiredService<TelemetryDbContext>(),
            [last.Builder(_telemetry).Build(sample, markerMessageId)],
            cancellationToken);
    }

    internal static async Task<HistoryMachineData> ReadDataAsync(
        ServiceHost telemetry,
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        using var scope = telemetry.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        var registration = await dbContext.MachineRegistrations
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == machineId, cancellationToken);
        var schema = await dbContext.MetricSchemaRevisions
            .AsNoTracking()
            .Where(revision => revision.MachineRegistrationId == machineId)
            .OrderByDescending(revision => revision.Revision)
            .FirstAsync(cancellationToken);
        var metrics = await dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.MachineRegistrationId == machineId)
            .ToListAsync(cancellationToken);
        var statuses = await dbContext.StatusDefinitions
            .AsNoTracking()
            .Where(status => status.MachineRegistrationId == machineId)
            .ToListAsync(cancellationToken);

        return new HistoryMachineData
        {
            RegistrationId = registration.Id,
            CompanyId = registration.CompanyId,
            GatewayId = registration.GatewayId,
            ExpectedGoLiveOn = registration.ExpectedGoLiveOn,
            SchemaRevision = schema.Revision,
            SchemaJson = schema.Schema,
            MetricIdsByKey = metrics.ToDictionary(metric => metric.Key, metric => metric.Id, StringComparer.Ordinal),
            StatusDefinitions = statuses
                .Select(status => new StatusDefinitionKey { Id = status.Id, Code = status.Code })
                .ToList(),
            Definitions = new MachineDefinitions
            {
                Metrics = metrics
                    .Select(metric => new MetricInfo
                    {
                        Key = metric.Key,
                        ValueKind = metric.ValueKind,
                        Min = metric.Min,
                        Max = metric.Max,
                        States = metric.States,
                    })
                    .ToList(),
                StatusCodes = statuses.Select(status => status.Code).ToList(),
            },
        };
    }

    private static List<StateHistoryEntry> ResolveChanges(
        string machine,
        TelemetryFile file,
        MachinesFile machinesFile,
        SeedContext context,
        DateTime windowStart
    ) =>
    [
        .. machinesFile.StateChanges
            .Where(change => change.Machine == machine)
            .Select(change => Entry(change.State, change.At, context)),
        .. file.Drains
            .Where(drain => drain.Machine == machine)
            .SelectMany(drain => new[]
            {
                Entry(MachineState.InMaintenance, drain.Maintenance.From, context),
                Entry(MachineState.Operational, drain.Maintenance.Until, context),
            }),
    ];

    private static StateHistoryEntry Entry(MachineState state, RelativeMoment at, SeedContext context) =>
        new() { State = state, StartedAt = SeedCalendar.ToUtc(at, context.Anchor, context.Zone).UtcDateTime };

    private sealed class MachineRun
    {
        private HistoryMessageBuilder? _builder;

        public required string Key { get; init; }

        public required Guid MachineId { get; init; }

        public required HistoryMachineData Data { get; init; }

        public required HistorySimulation Simulation { get; init; }

        public HistoryMessageBuilder Builder(ServiceHost telemetry) =>
            _builder ??= new HistoryMessageBuilder(telemetry.GetService<IMachineMessageParser>(), Data);
    }
}
