namespace HealthyFac.Core.Domain.Drafts.Proposals;

public static class DraftProposalInvariantErrors
{
    public static InvalidOperationException EmptyId(string name) =>
        new($"The draft proposal needs a non-empty {name}.");
}
