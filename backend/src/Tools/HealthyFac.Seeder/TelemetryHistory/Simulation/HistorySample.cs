using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.Seeder.TelemetryHistory.Simulation;

/// <summary>One sample of the simulated history. The caller builds the payload from it.</summary>
public sealed record HistorySample
{
    /// <summary>The UTC sampling time, on a whole multiple of the interval after the window start.</summary>
    public required DateTime SampledAt { get; init; }

    public required ScenarioSample Sample { get; init; }
}
