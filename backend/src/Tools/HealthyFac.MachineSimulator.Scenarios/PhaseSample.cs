namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The values of one running phase.
/// </summary>
public sealed record PhaseSample
{
    /// <summary>The metric values by key.</summary>
    public required IReadOnlyDictionary<string, object> Values { get; init; }

    /// <summary>The status code of the phase, or <see langword="null"/>.</summary>
    public string? InfoCode { get; init; }

    /// <summary>Whether the phase is the main phase, where a drained value replaces the phase value.</summary>
    public required bool IsMain { get; init; }

    /// <summary>The keys of the counters that grow in the phase.</summary>
    public required IReadOnlyList<string> GrowingCounters { get; init; }
}
