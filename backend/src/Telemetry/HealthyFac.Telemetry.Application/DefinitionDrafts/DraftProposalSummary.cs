using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// The origin of a definition draft as the API shows it. A draft has the fields of its own source only.
/// </summary>
public record DraftProposalSummary
{
    public required DraftSource Source { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? ChatId { get; init; }
}
