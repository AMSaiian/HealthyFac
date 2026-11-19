namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// The values of one <c>SignalEpisodeChanged</c> message that <see cref="MachineSignal"/> stores. It carries no
/// service message type, because the Domain layer references no message project.
/// </summary>
public sealed record SignalChange
{
    public required SignalRule Rule { get; init; }

    public required SignalSeverity Severity { get; init; }

    public required SignalState State { get; init; }

    public Guid? MetricDefinitionId { get; init; }

    public string? MetricKey { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public required int Sequence { get; init; }
}
