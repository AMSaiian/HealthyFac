using HealthyFac.Core.Domain.Drafts.Proposals;

namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// The origin of a draft as the API shows it. A draft has the fields of its own source only.
/// </summary>
public record DraftProposalSummary
{
    public required DraftSource Source { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? ChatId { get; init; }
}
