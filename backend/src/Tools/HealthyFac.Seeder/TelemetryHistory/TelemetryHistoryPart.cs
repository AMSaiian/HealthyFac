using HealthyFac.Core.Application.Machines.States;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Application.Evaluation;
using Microsoft.Extensions.Options;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// Seeds the telemetry history of the 3 machines from <c>telemetry.json</c>: the history steps, the maintenance state
/// changes of the drains, and the marker. The seed check reads the events like those of every other part.
/// </summary>
public sealed class TelemetryHistoryPart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly Func<int> _readCatchUpBatchReadings;
    private readonly Dictionary<string, Func<SeedContext, CancellationToken, Task>> _actions = [];

    private TelemetryFile _file = null!;
    private MachinesFile _machinesFile = null!;
    private IReadOnlyDictionary<string, HistoryMachine> _machines = null!;
    private HistoryRun? _run;

    public TelemetryHistoryPart(ServiceHosts.ServiceHosts hosts)
        : this(hosts, () => hosts.Telemetry.GetService<IOptions<DetectionOptions>>().Value.CatchUpBatchReadings)
    {
    }

    /// <summary>Creates the part with its own reader of the batch size, for the tests that have no host.</summary>
    /// <param name="hosts">The service hosts that the state changes go through.</param>
    /// <param name="readCatchUpBatchReadings">Reads <c>CatchUpBatchReadings</c> of the detection settings.</param>
    internal TelemetryHistoryPart(ServiceHosts.ServiceHosts hosts, Func<int> readCatchUpBatchReadings)
    {
        _hosts = hosts;
        _readCatchUpBatchReadings = readCatchUpBatchReadings;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        var file = folder.Read<TelemetryFile>(TelemetryHistoryConstants.FileName);
        var machinesFile = folder.Read<MachinesFile>(MachinesConstants.FileName);
        var machines = ReadMachines(folder, machinesFile);
        _file = file;
        _machinesFile = machinesFile;
        _machines = machines;
        _run = null;
        TelemetryHistoryChecks.Run(
            file,
            machines,
            folder.Read<CompanyFile>(PeopleConstants.FileName).People.Select(person => person.Key).ToHashSet());
        _actions.Clear();

        return
        [
            .. ReadHistorySteps(file, machinesFile, machines),
            .. file.Drains.SelectMany(MaintenanceEvents),
            MarkerEvent(machines),
        ];
    }

    /// <summary>
    /// Runs one event. A history step writes the samples up to its moment and evaluates the machines. The marker
    /// writes the anchor sample of the last machine with the ID of the marker, after the last evaluation. A state
    /// change of a maintenance goes through the Core use case.
    /// </summary>
    /// <remarks>
    /// The form <b>Resume</b> of the cancellation rule is not used: a cancelled run has no marker message, so the next
    /// run reads a seed without the history, and the reset starts it again.
    /// </remarks>
    /// <param name="seedEvent">An event that <see cref="ReadEvents"/> listed.</param>
    /// <param name="context">The keys and the actors of the job.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the event is stored.</returns>
    public async Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken)
    {
        var key = SeedItem.KeyOf(seedEvent.Item);
        if (_actions.TryGetValue(key, out var action))
        {
            await action(context, cancellationToken);

            return;
        }

        _run ??= await HistoryRun.CreateAsync(
            _hosts.Telemetry, _file, _machinesFile, _machines.Values.ToList(), context, cancellationToken);
        if (key == TelemetryHistoryConstants.MarkerKey)
        {
            await _run.WriteMarkerAsync(_file.MarkerMessageId, cancellationToken);

            return;
        }

        await _run.RunStepAsync(
            SeedCalendar.ToUtc(seedEvent.At, context.Anchor, context.Zone).UtcDateTime, cancellationToken);
    }

    /// <summary>
    /// Finds the largest step in minutes whose samples fit in one catch-up batch of the evaluator, at least 1 minute.
    /// </summary>
    /// <param name="sampleIntervalSeconds">The time between two samples of a machine.</param>
    /// <param name="catchUpBatchReadings">The readings that one catch-up batch of the evaluator reads.</param>
    /// <param name="definitionCount">The largest number of metrics and statuses of one machine.</param>
    /// <returns>The step in minutes.</returns>
    internal static int StepMinutes(int sampleIntervalSeconds, int catchUpBatchReadings, int definitionCount) =>
        Math.Max(
            val1: 1,
            sampleIntervalSeconds
            * catchUpBatchReadings
            / (RelativeMoment.MinutesPerHour * definitionCount));

    /// <summary>Builds the item name of a maintenance state change of a drain.</summary>
    /// <param name="drain">The drain.</param>
    /// <param name="state">The state that the change sets.</param>
    /// <returns>The item name.</returns>
    internal static string MaintenanceItemOf(DrainSeed drain, MachineState state) =>
        SeedItem.Of(TelemetryHistoryConstants.FileName, MaintenanceKeyOf(drain, state));

    private static string MaintenanceKeyOf(DrainSeed drain, MachineState state) =>
        $"{drain.Key}{TelemetryHistoryConstants.MaintenanceSuffix}.{state.Value}";

    private static string MachineKey(string key) => $"{key}{MachinesConstants.MachineSuffix}";

    private static string UserKey(string person) => $"{person}{PeopleConstants.UserSuffix}";

    private static SeedRequest StateRequest(MachineState state) =>
        new()
        {
            Service = ServiceName.Core,
            Request = new ChangeMachineStateRequest
            {
                CompanyId = PeopleConstants.CheckId,
                Id = PeopleConstants.CheckId,
                State = state,
            },
        };

    private static IReadOnlyDictionary<string, HistoryMachine> ReadMachines(
        SeedFolder folder,
        MachinesFile machinesFile
    )
    {
        var drafts = folder.Read<IngestSetFile>(MachinesConstants.SetFileName)
            .Entries
            .SelectMany(entry => entry.DraftsProposed.Machines)
            .ToDictionary(machine => machine.DraftId);

        return machinesFile.Machines
            .Select((machine, index) => new HistoryMachine
            {
                Key = machine.Key,
                Index = index,
                Draft = FindDraft(drafts, machine),
                Scenario = ScenarioBinding.Create(
                    ScenarioBinding.KindOf(FindDraft(drafts, machine).SerialNumber ?? string.Empty)),
            })
            .ToDictionary(machine => machine.Key);
    }

    private static ProposedMachine FindDraft(IReadOnlyDictionary<Guid, ProposedMachine> drafts, MachineSeed machine) =>
        drafts.TryGetValue(machine.DraftId, out var draft)
            ? draft
            : throw new SeedFileException(
                MachinesErrors.DraftUnknown(
                    SeedItem.Of(MachinesConstants.FileName, $"{machine.Key}{MachinesConstants.DefinitionsSuffix}"),
                    machine.DraftId));

    private static SeedEvent MarkerEvent(IReadOnlyDictionary<string, HistoryMachine> machines) =>
        new()
        {
            At = RelativeMoment.FromTotalMinutes(totalMinutes: 0),
            Item = SeedItem.Of(TelemetryHistoryConstants.FileName, TelemetryHistoryConstants.MarkerKey),
            Uses = machines.Keys.Select(MachineKey).ToList(),
        };

    private static void AddStep(
        List<SeedEvent> events,
        HashSet<string> keys,
        RelativeMoment moment,
        IReadOnlyDictionary<string, HistoryMachine> machines
    )
    {
        var key = $"{TelemetryHistoryConstants.StepPrefix}.{moment}";
        if (!keys.Add(key))
        {
            return;
        }

        events.Add(
            new SeedEvent
            {
                At = moment,
                Item = SeedItem.Of(TelemetryHistoryConstants.FileName, key),
                Uses = machines.Keys.Select(MachineKey).ToList(),
            });
    }

    private List<SeedEvent> ReadHistorySteps(
        TelemetryFile file,
        MachinesFile machinesFile,
        IReadOnlyDictionary<string, HistoryMachine> machines
    )
    {
        var stepMinutes = StepMinutes(
            file.SampleIntervalSeconds,
            _readCatchUpBatchReadings(),
            machines.Values.Max(machine => machine.Draft.Metrics.Count + machine.Draft.Statuses.Count));
        var events = new List<SeedEvent>();
        var keys = new HashSet<string>();

        for (var minute = -file.WindowHours * RelativeMoment.MinutesPerHour;
            minute < 0;
            minute += stepMinutes)
        {
            AddStep(events, keys, RelativeMoment.FromTotalMinutes(minute), machines);
        }

        AddStep(events, keys, RelativeMoment.FromTotalMinutes(totalMinutes: 0), machines);
        foreach (var moment in machinesFile.StateChanges.Select(change => change.At)
            .Concat(file.Drains.SelectMany(MaintenanceMoments)))
        {
            AddStep(events, keys, moment, machines);
        }

        return events;
    }

    private static IEnumerable<RelativeMoment> MaintenanceMoments(DrainSeed drain) =>
        [drain.Maintenance.From, drain.Maintenance.Until];

    private IEnumerable<SeedEvent> MaintenanceEvents(DrainSeed drain)
    {
        yield return StateEvent(drain, MachineState.InMaintenance, drain.Maintenance.From);
        yield return StateEvent(drain, MachineState.Operational, drain.Maintenance.Until);
    }

    private SeedEvent StateEvent(DrainSeed drain, MachineState state, RelativeMoment at)
    {
        var key = MaintenanceKeyOf(drain, state);
        _actions[key] = (context, cancellationToken) => ChangeStateAsync(drain, state, context, cancellationToken);

        return new SeedEvent
        {
            At = at,
            Actor = drain.Maintenance.By,
            Item = SeedItem.Of(TelemetryHistoryConstants.FileName, key),
            Uses = [MachineKey(drain.Machine), UserKey(drain.Maintenance.By)],
            Requests = [StateRequest(state)],
        };
    }

    private async Task ChangeStateAsync(
        DrainSeed drain,
        MachineState state,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        await _hosts.Core.SendAsync(
            context.GetActor(drain.Maintenance.By),
            new ChangeMachineStateRequest
            {
                CompanyId = context.GetId(PeopleConstants.CompanyKey),
                Id = context.GetId(MachineKey(drain.Machine)),
                State = state,
            },
            cancellationToken);
    }
}
