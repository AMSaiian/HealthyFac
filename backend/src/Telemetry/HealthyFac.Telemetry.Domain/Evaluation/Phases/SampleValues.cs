namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// The values of one sample time of a machine: the numeric values, the discrete values, and the active statuses. A
/// metric without a value in the sample is absent from the dictionaries.
/// </summary>
public sealed record SampleValues
{
    public IReadOnlyDictionary<Guid, double> Numeric { get; init; } = new Dictionary<Guid, double>();

    /// <summary>The discrete values by metric: <c>"true"</c> or <c>"false"</c> for a boolean, the state code for a
    /// state set.</summary>
    public IReadOnlyDictionary<Guid, string> Discrete { get; init; } = new Dictionary<Guid, string>();

    public IReadOnlySet<Guid> ActiveStatusIds { get; init; } = new HashSet<Guid>();
}
