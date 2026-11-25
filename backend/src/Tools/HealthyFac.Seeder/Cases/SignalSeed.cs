using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Seeder.Cases;

public sealed class SignalSeed
{
    public required Guid Id { get; init; }

    public required SignalRule Rule { get; init; }

    public required SignalSeverity Severity { get; init; }

    public required SignalState State { get; init; }

    public required SignalCoverage Coverage { get; init; }

    public required string MetricKey { get; init; }

    public required double Value { get; init; }

    public required double Limit { get; init; }
}
