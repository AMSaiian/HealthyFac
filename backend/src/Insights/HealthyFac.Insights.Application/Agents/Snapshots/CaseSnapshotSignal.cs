using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Insights.Application.Agents.Snapshots;

/// <summary>
/// One signal of a <see cref="CaseSnapshot"/>.
/// </summary>
public sealed record CaseSnapshotSignal
{
    public required string Rule { get; init; }

    public required string Severity { get; init; }

    public required string State { get; init; }

    public required string Coverage { get; init; }

    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public static CaseSnapshotSignal From(MachineSignal signal) =>
        new()
        {
            Rule = signal.Rule.Value,
            Severity = signal.Severity.Value,
            State = signal.State.Value,
            Coverage = signal.Coverage.Value,
            MetricKey = signal.MetricKey,
            StatusCode = signal.StatusCode,
            PhaseCode = signal.PhaseCode,
            Value = signal.Value,
            Limit = signal.Limit,
            Z = signal.Z,
            ForecastSeconds = signal.ForecastSeconds,
            OpenedAt = signal.OpenedAt,
            ResolvedAt = signal.ResolvedAt,
        };
}
