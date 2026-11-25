using HealthyFac.Core.Domain.Machines;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.SeedFiles;

/// <summary>
/// The rules of the telemetry script that need the anchor: the state changes of one machine alternate, and no event
/// of <c>telemetry.json</c> overlaps a maintenance of its machine. A moment that does not resolve is left to the
/// failure that the moment check of <see cref="SeedCheck"/> already reports.
/// </summary>
internal static class TelemetryScriptCheck
{
    /// <summary>Checks the script.</summary>
    /// <param name="script">The telemetry script and the machines.</param>
    /// <param name="anchor">The start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <returns>The failures. The list is empty when the rules hold.</returns>
    public static IReadOnlyList<SeedCheckFailure> Run(TelemetryScript script, DateTimeOffset anchor, TimeZoneInfo zone)
    {
        try
        {
            var failures = new List<SeedCheckFailure>();
            var maintenances = FindMaintenances(script, anchor, zone, failures);
            failures.AddRange(
                FindSpans(script, anchor, zone)
                    .Select(span => (Span: span, Maintenance: FindOverlap(span, maintenances)))
                    .Where(entry => entry.Maintenance is not null)
                    .Select(entry => Failure(
                        entry.Span.Item,
                        SeedFileErrors.EventOverlapsMaintenance(entry.Span.Machine, entry.Maintenance!.StartItem))));

            return failures;
        }
        catch (MomentException)
        {
            return [];
        }
    }

    private static List<Maintenance> FindMaintenances(
        TelemetryScript script,
        DateTimeOffset anchor,
        TimeZoneInfo zone,
        List<SeedCheckFailure> failures
    )
    {
        var changes = script.Machines.StateChanges
            .Select(change => new Change
            {
                Machine = change.Machine,
                At = SeedCalendar.ToUtc(change.At, anchor, zone),
                State = change.State,
                Item = MachinesPart.StateItemOf(change),
            })
            .Concat(script.Telemetry.Drains.SelectMany(drain => DrainChanges(drain, anchor, zone)))
            .GroupBy(change => change.Machine);
        var maintenances = new List<Maintenance>();
        foreach (var machineChanges in changes)
        {
            var state = MachineState.Operational;
            Change? start = null;
            foreach (var change in machineChanges.OrderBy(candidate => candidate.At))
            {
                if (change.State == state)
                {
                    failures.Add(
                        Failure(change.Item, SeedFileErrors.StateDoesNotAlternate(change.Machine, state.Value)));

                    continue;
                }

                state = change.State;
                if (state == MachineState.InMaintenance)
                {
                    start = change;
                }
                else if (start is not null)
                {
                    maintenances.Add(Close(start, change.At));
                    start = null;
                }
            }

            if (start is not null)
            {
                maintenances.Add(Close(start, anchor));
            }
        }

        return maintenances;
    }

    private static IEnumerable<Change> DrainChanges(DrainSeed drain, DateTimeOffset anchor, TimeZoneInfo zone) =>
    [
        new()
        {
            Machine = drain.Machine,
            At = SeedCalendar.ToUtc(drain.Maintenance.From, anchor, zone),
            State = MachineState.InMaintenance,
            Item = TelemetryHistoryPart.MaintenanceItemOf(drain, MachineState.InMaintenance),
        },
        new()
        {
            Machine = drain.Machine,
            At = SeedCalendar.ToUtc(drain.Maintenance.Until, anchor, zone),
            State = MachineState.Operational,
            Item = TelemetryHistoryPart.MaintenanceItemOf(drain, MachineState.Operational),
        },
    ];

    private static IEnumerable<Span> FindSpans(TelemetryScript script, DateTimeOffset anchor, TimeZoneInfo zone)
    {
        var telemetry = script.Telemetry;

        return telemetry.Drains
            .Select(drain => new Span
            {
                Item = ItemOf(drain.Key),
                Machine = drain.Machine,
                From = SeedCalendar.ToUtc(drain.From, anchor, zone),
                Until = SeedCalendar.ToUtc(drain.Maintenance.Until, anchor, zone),
                OwnStartItem = TelemetryHistoryPart.MaintenanceItemOf(drain, MachineState.InMaintenance),
            })
            .Concat(telemetry.Steps.Select(step => Plain(step.Key, step.Machine, step.From, step.Until, anchor, zone)))
            .Concat(telemetry.Gaps.Select(gap => Plain(gap.Key, gap.Machine, gap.From, gap.Until, anchor, zone)))
            .Concat(
                telemetry.Overrides.Select(overrideSeed => Plain(
                    overrideSeed.Key, overrideSeed.Machine, overrideSeed.From, overrideSeed.Until, anchor, zone)));
    }

    private static Span Plain(
        string key,
        string machine,
        RelativeMoment from,
        RelativeMoment until,
        DateTimeOffset anchor,
        TimeZoneInfo zone
    ) =>
        new()
        {
            Item = ItemOf(key),
            Machine = machine,
            From = SeedCalendar.ToUtc(from, anchor, zone),
            Until = SeedCalendar.ToUtc(until, anchor, zone),
            OwnStartItem = null,
        };

    private static Maintenance? FindOverlap(Span span, IEnumerable<Maintenance> maintenances) =>
        maintenances.FirstOrDefault(maintenance => maintenance.Machine == span.Machine
            && maintenance.StartItem != span.OwnStartItem
            && span.From < maintenance.Until
            && maintenance.From < span.Until);

    private static Maintenance Close(Change start, DateTimeOffset until) =>
        new() { Machine = start.Machine, From = start.At, Until = until, StartItem = start.Item };

    private static string ItemOf(string key) => SeedItem.Of(TelemetryHistoryConstants.FileName, key);

    private static SeedCheckFailure Failure(string item, string rule) =>
        new() { File = SeedItem.FileOf(item), Item = item, Rule = rule };

    private sealed class Change
    {
        public required string Machine { get; init; }

        public required DateTimeOffset At { get; init; }

        public required MachineState State { get; init; }

        public required string Item { get; init; }
    }

    private sealed class Maintenance
    {
        public required string Machine { get; init; }

        public required DateTimeOffset From { get; init; }

        public required DateTimeOffset Until { get; init; }

        public required string StartItem { get; init; }
    }

    private sealed class Span
    {
        public required string Item { get; init; }

        public required string Machine { get; init; }

        public required DateTimeOffset From { get; init; }

        public required DateTimeOffset Until { get; init; }

        public required string? OwnStartItem { get; init; }
    }
}
