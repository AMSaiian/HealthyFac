using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Infrastructure.Agents.Runs;

/// <summary>
/// A run that the worker claimed: the identity to run it as.
/// </summary>
public record ClaimedRun
{
    public required Guid RunId { get; init; }

    public required Guid CompanyId { get; init; }

    public required AgentRole Role { get; init; }
}
