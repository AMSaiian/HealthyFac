using System.Text.Json;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// The check rules of <c>telemetry.json</c> that need no anchor. Each failure is a <see cref="SeedFileException"/>
/// with the item and the rule. The rules that need the anchor run in the seed check.
/// </summary>
internal static class TelemetryHistoryChecks
{
    private const string WindowHoursItem = "windowHours";
    private const string SampleIntervalItem = "sampleIntervalSeconds";

    /// <summary>Checks the whole file.</summary>
    /// <param name="file">The content of <c>telemetry.json</c>.</param>
    /// <param name="machines">The machines of <c>machines.json</c> by key.</param>
    /// <param name="people">The keys of the people of <c>company.json</c>.</param>
    /// <exception cref="SeedFileException">A rule fails.</exception>
    public static void Run(
        TelemetryFile file,
        IReadOnlyDictionary<string, HistoryMachine> machines,
        ISet<string> people
    )
    {
        if (file.WindowHours <= 0)
        {
            throw Fail(WindowHoursItem, TelemetryHistoryErrors.WindowInvalid);
        }

        if (file.SampleIntervalSeconds <= 0)
        {
            throw Fail(SampleIntervalItem, TelemetryHistoryErrors.WindowInvalid);
        }

        CheckKeysAreUnique(file);

        foreach (var drain in file.Drains)
        {
            CheckDrain(file, drain, machines, people);
        }

        foreach (var step in file.Steps)
        {
            CheckStep(file, step, machines);
        }

        foreach (var gap in file.Gaps)
        {
            var item = ItemOf(gap.Key);
            FindMachine(item, gap.Machine, machines);
            CheckSpan(file, item, gap.From, gap.Until);
        }

        foreach (var overrideSeed in file.Overrides)
        {
            CheckOverride(file, overrideSeed, machines);
        }
    }

    /// <summary>Finds the moment of the link: 1 minute before the maintenance start.</summary>
    /// <param name="drain">The drain.</param>
    /// <returns>The moment in the hour form.</returns>
    internal static RelativeMoment LinkMoment(DrainSeed drain) =>
        RelativeMoment.FromTotalMinutes(
            drain.Maintenance.From.TotalMinutes - TelemetryHistoryConstants.LinkLeadMinutes);

    private static void CheckKeysAreUnique(TelemetryFile file)
    {
        var listsByKey = new Dictionary<string, string>();
        var keys = file.Drains.Select(drain => (drain.Key, List: TelemetryHistoryConstants.DrainsList))
            .Concat(file.Steps.Select(step => (step.Key, List: TelemetryHistoryConstants.StepsList)))
            .Concat(file.Gaps.Select(gap => (gap.Key, List: TelemetryHistoryConstants.GapsList)))
            .Concat(
                file.Overrides.Select(overrideSeed =>
                    (overrideSeed.Key, List: TelemetryHistoryConstants.OverridesList)));
        foreach (var (key, list) in keys)
        {
            if (!listsByKey.TryAdd(key, list))
            {
                throw new SeedFileException(TelemetryHistoryErrors.KeyDuplicated(ItemOf(key), listsByKey[key], list));
            }
        }
    }

    private static string ItemOf(string key) => SeedItem.Of(TelemetryHistoryConstants.FileName, key);

    private static SeedFileException Fail(string key, Func<string, string> message) =>
        new(message(ItemOf(key)));

    private static HistoryMachine FindMachine(
        string item,
        string key,
        IReadOnlyDictionary<string, HistoryMachine> machines
    ) =>
        machines.TryGetValue(key, out var machine)
            ? machine
            : throw new SeedFileException(TelemetryHistoryErrors.MachineUnknown(item, key));

    private static ProposedMetric FindMetric(string item, string key, HistoryMachine machine) =>
        machine.Draft.Metrics.FirstOrDefault(metric => metric.Key == key)
        ?? throw new SeedFileException(TelemetryHistoryErrors.MetricUnknown(item, key, machine.Key));

    private static void CheckMoment(TelemetryFile file, string item, RelativeMoment moment)
    {
        if (moment.Form != MomentForm.Hour)
        {
            throw new SeedFileException(TelemetryHistoryErrors.MomentNotInHours(item, moment));
        }

        var minutes = moment.TotalMinutes;
        if (minutes > 0 || minutes < -file.WindowHours * RelativeMoment.MinutesPerHour)
        {
            throw new SeedFileException(TelemetryHistoryErrors.MomentOutsideWindow(item, moment, file.WindowHours));
        }
    }

    private static void CheckSpan(TelemetryFile file, string item, RelativeMoment from, RelativeMoment until)
    {
        CheckMoment(file, item, from);
        CheckMoment(file, item, until);
        if (until.TotalMinutes <= from.TotalMinutes)
        {
            throw new SeedFileException(TelemetryHistoryErrors.EndNotAfterStart(item));
        }
    }

    private static void CheckDrain(
        TelemetryFile file,
        DrainSeed drain,
        IReadOnlyDictionary<string, HistoryMachine> machines,
        ISet<string> people
    )
    {
        var item = ItemOf(drain.Key);
        var machine = FindMachine(item, drain.Machine, machines);
        if (machine.Scenario.Modules.All(module => module.Name.Value != drain.Module))
        {
            throw new SeedFileException(TelemetryHistoryErrors.ModuleUnknown(item, drain.Module, drain.Machine));
        }

        if (!people.Contains(drain.Maintenance.By))
        {
            throw new SeedFileException(TelemetryHistoryErrors.PersonUnknown(item, drain.Maintenance.By));
        }

        if (drain.DrainHours <= 0)
        {
            throw new SeedFileException(TelemetryHistoryErrors.DrainHoursInvalid(item));
        }

        CheckMoment(file, item, drain.From);
        CheckSpan(file, item, drain.Maintenance.From, drain.Maintenance.Until);
        CheckWorkOrder(file, drain);
        var drainMinutes = drain.DrainHours * RelativeMoment.MinutesPerHour;
        var sinceStart = drain.Maintenance.From.TotalMinutes - drain.From.TotalMinutes;
        if (sinceStart < drainMinutes * ModuleState.AlarmProgress || sinceStart >= drainMinutes)
        {
            throw new SeedFileException(TelemetryHistoryErrors.MaintenanceOutsideDrainRange(item));
        }
    }

    private static void CheckWorkOrder(TelemetryFile file, DrainSeed drain)
    {
        if (drain.WorkOrder is not { } workOrder)
        {
            return;
        }

        var item = ItemOf($"{drain.Key}{TelemetryHistoryConstants.WorkOrderSuffix}");
        CheckMoment(file, item, workOrder.CreatedAt);
        var assignedMinutes = workOrder.CreatedAt.TotalMinutes + TelemetryHistoryConstants.AssignDelayMinutes;
        if (assignedMinutes >= LinkMoment(drain).TotalMinutes)
        {
            throw new SeedFileException(TelemetryHistoryErrors.WorkOrderNotBeforeLink(item));
        }
    }

    private static void CheckStep(
        TelemetryFile file,
        StepSeed step,
        IReadOnlyDictionary<string, HistoryMachine> machines
    )
    {
        var item = ItemOf(step.Key);
        var machine = FindMachine(item, step.Machine, machines);
        var metric = FindMetric(item, step.Metric, machine);
        if (metric.ValueKind != MetricValueKind.Numeric.Value)
        {
            throw new SeedFileException(TelemetryHistoryErrors.StepMetricNotNumeric(item, step.Metric));
        }

        CheckSpan(file, item, step.From, step.Until);
    }

    private static void CheckOverride(
        TelemetryFile file,
        OverrideSeed overrideSeed,
        IReadOnlyDictionary<string, HistoryMachine> machines
    )
    {
        var item = ItemOf(overrideSeed.Key);
        var machine = FindMachine(item, overrideSeed.Machine, machines);
        foreach (var (key, value) in overrideSeed.Values)
        {
            var metric = FindMetric(item, key, machine);
            if (!IsFit(metric, value))
            {
                throw new SeedFileException(
                    TelemetryHistoryErrors.OverrideValueInvalid(item, key, metric.ValueKind ?? string.Empty));
            }
        }

        var unknownCode = overrideSeed.Codes.FirstOrDefault(code => !HasStatusCode(machine, code));
        if (unknownCode is not null)
        {
            throw new SeedFileException(TelemetryHistoryErrors.StatusCodeUnknown(item, unknownCode, machine.Key));
        }

        CheckSpan(file, item, overrideSeed.From, overrideSeed.Until);
    }

    private static bool HasStatusCode(HistoryMachine machine, string code) =>
        machine.Draft.Statuses.Any(status => string.Equals(status.Code, code, StringComparison.Ordinal));

    private static bool IsFit(ProposedMetric metric, JsonElement value) =>
        metric.ValueKind == MetricValueKind.Numeric.Value
            ? value.ValueKind == JsonValueKind.Number
            : metric.ValueKind == MetricValueKind.Boolean.Value
                ? value.ValueKind is JsonValueKind.True or JsonValueKind.False
                : metric.ValueKind == MetricValueKind.StateSet.Value
                && value.ValueKind == JsonValueKind.String
                && metric.States?.Contains(value.GetString()!) == true;
}
