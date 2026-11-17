namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// One sample of a scenario: the metric values, the status codes, and what grows. A value is a <see cref="double"/>,
/// a <see cref="bool"/>, or a <see cref="string"/>.
/// </summary>
public sealed record ScenarioSample
{
    /// <summary>The metric values by key.</summary>
    public required IReadOnlyDictionary<string, object> Values { get; init; }

    /// <summary>The status codes of this sample.</summary>
    public required IReadOnlyList<string> StatusCodes { get; init; }

    /// <summary>
    /// <see langword="true"/> while the machine runs. Only then the run time, the counters, and the drain of the
    /// modules grow.
    /// </summary>
    public required bool IsRunning { get; init; }

    /// <summary>The keys of the counters that grow in this sample, when <see cref="IsRunning"/> is true.</summary>
    public IReadOnlyList<string> GrowingCounters { get; init; } = [];
}
