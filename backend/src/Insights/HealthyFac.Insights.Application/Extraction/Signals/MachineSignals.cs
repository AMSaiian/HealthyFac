namespace HealthyFac.Insights.Application.Extraction.Signals;

/// <summary>
/// The metric definitions and the status definitions of a machine in Telemetry.
/// </summary>
public record MachineSignals
{
    public required IReadOnlyList<SignalReference> Metrics { get; init; }

    public required IReadOnlyList<SignalReference> Statuses { get; init; }

    public IEnumerable<string> MetricKeys => Metrics.Select(metric => metric.Key);

    public IEnumerable<string> StatusCodes => Statuses.Select(status => status.Key);
}

/// <summary>
/// One definition of a machine: its ID and its key (a metric key or a status code).
/// </summary>
public sealed record SignalReference
{
    /// <summary>The ID of the definition in Telemetry.</summary>
    public required Guid Id { get; init; }

    /// <summary>The metric key or the status code.</summary>
    public required string Key { get; init; }
}
