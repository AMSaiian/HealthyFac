using System.Text.Json;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory.Simulation;

/// <summary>
/// The events of <c>telemetry.json</c> of one machine, resolved on the anchor. The history simulation asks it to start
/// the drains, to find a gap, and to shape each sample. The events draw no random value, so a sample outside every
/// event equals a sample of a run without the events.
/// </summary>
public sealed class HistoryEvents
{
    private readonly List<DrainStart> _drains;
    private readonly List<StepWindow> _steps;
    private readonly List<OverrideWindow> _overrides;
    private readonly List<GapWindow> _gaps;
    private int _startedDrains;

    /// <summary>Resolves the events.</summary>
    /// <param name="drains">The drains of the machine.</param>
    /// <param name="steps">The steps of the machine.</param>
    /// <param name="gaps">The gaps of the machine.</param>
    /// <param name="overrides">The overrides of the machine.</param>
    /// <param name="anchor">The start of the job.</param>
    /// <param name="zone">The company zone.</param>
    public HistoryEvents(
        IEnumerable<DrainSeed> drains,
        IEnumerable<StepSeed> steps,
        IEnumerable<GapSeed> gaps,
        IEnumerable<OverrideSeed> overrides,
        DateTimeOffset anchor,
        TimeZoneInfo zone
    )
    {
        _drains = drains
            .Select(drain => new DrainStart
            {
                At = Resolve(drain.From, anchor, zone),
                Module = ModuleName.FromValue(drain.Module),
                Duration = TimeSpan.FromHours(drain.DrainHours),
            })
            .OrderBy(drain => drain.At)
            .ToList();
        _steps = steps
            .Select(step => new StepWindow
            {
                Metric = step.Metric,
                Offset = step.Offset,
                From = Resolve(step.From, anchor, zone),
                Until = Resolve(step.Until, anchor, zone),
            })
            .ToList();
        _gaps = gaps
            .Select(gap => new GapWindow
            {
                From = Resolve(gap.From, anchor, zone),
                Until = Resolve(gap.Until, anchor, zone),
            })
            .ToList();
        _overrides = overrides
            .Select(overrideSeed => new OverrideWindow
            {
                Values = overrideSeed.Values.ToDictionary(pair => pair.Key, pair => ToValue(pair.Value)),
                Codes = overrideSeed.Codes,
                From = Resolve(overrideSeed.From, anchor, zone),
                Until = Resolve(overrideSeed.Until, anchor, zone),
            })
            .ToList();
    }

    /// <summary>Starts every drain whose moment has come and that did not start yet.</summary>
    /// <param name="machine">The machine of the drains.</param>
    /// <param name="at">The UTC time of the tick.</param>
    public void StartDrains(SimulatedMachine machine, DateTime at)
    {
        while (_startedDrains < _drains.Count && _drains[_startedDrains].At <= at)
        {
            var drain = _drains[_startedDrains++];
            machine.TrySetMode(drain.Module, ModuleMode.Draining, drain.Duration);
        }
    }

    /// <summary>Tells whether the machine sends no message at a time.</summary>
    /// <param name="at">The UTC time of the tick.</param>
    /// <returns><see langword="true"/> inside a gap.</returns>
    public bool IsGap(DateTime at) => _gaps.Any(gap => gap.Contains(at));

    /// <summary>Adds the steps and applies the overrides that hold at a time.</summary>
    /// <param name="sample">The sample of the scenario.</param>
    /// <param name="at">The UTC time of the tick.</param>
    /// <returns>The same sample when no event holds, else a changed copy.</returns>
    public ScenarioSample Shape(ScenarioSample sample, DateTime at)
    {
        var activeSteps = _steps.Where(step => step.Contains(at)).ToList();
        var activeOverrides = _overrides.Where(overrideWindow => overrideWindow.Contains(at)).ToList();
        if (activeSteps.Count == 0 && activeOverrides.Count == 0)
        {
            return sample;
        }

        var values = new Dictionary<string, object>(sample.Values);
        foreach (var step in activeSteps.Where(step => values.GetValueOrDefault(step.Metric) is double))
        {
            values[step.Metric] = (double)values[step.Metric] + step.Offset;
        }

        var codes = sample.StatusCodes;
        foreach (var overrideWindow in activeOverrides)
        {
            foreach (var (key, value) in overrideWindow.Values)
            {
                values[key] = value;
            }

            codes = overrideWindow.Codes;
        }

        return sample with { Values = values, StatusCodes = codes };
    }

    private static DateTime Resolve(RelativeMoment moment, DateTimeOffset anchor, TimeZoneInfo zone) =>
        SeedCalendar.ToUtc(moment, anchor, zone).UtcDateTime;

    private static object ToValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.GetString()!,
        };

    private sealed record DrainStart
    {
        public required DateTime At { get; init; }

        public required ModuleName Module { get; init; }

        public required TimeSpan Duration { get; init; }
    }

    private sealed record StepWindow
    {
        public required string Metric { get; init; }

        public required double Offset { get; init; }

        public required DateTime From { get; init; }

        public required DateTime Until { get; init; }

        public bool Contains(DateTime at) => From <= at && at < Until;
    }

    private sealed record OverrideWindow
    {
        public required IReadOnlyDictionary<string, object> Values { get; init; }

        public required IReadOnlyList<string> Codes { get; init; }

        public required DateTime From { get; init; }

        public required DateTime Until { get; init; }

        public bool Contains(DateTime at) => From <= at && at < Until;
    }

    private sealed record GapWindow
    {
        public required DateTime From { get; init; }

        public required DateTime Until { get; init; }

        public bool Contains(DateTime at) => From <= at && at < Until;
    }
}
