using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Insights.Application.Realtime;

public sealed record MaintenanceCaseOpenedData
{
    public required Guid CaseId { get; init; }

    public required SignalSeverity Severity { get; init; }
}
