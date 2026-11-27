using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;

public static class DraftProposalTestData
{
    public static TheoryData<string, Func<Guid, DraftProposal>> EmptyIdCases =>
        new()
        {
            {
                "ingest id",
                id => DraftProposal.ForIngest(id, ProposalEntryTestData.CompanyId, ProposalEntryTestData.FileId)
            },
            {
                "ingest company",
                id => DraftProposal.ForIngest(ProposalEntryTestData.AttemptId, id, ProposalEntryTestData.FileId)
            },
            {
                "ingest file",
                id => DraftProposal.ForIngest(ProposalEntryTestData.AttemptId, ProposalEntryTestData.CompanyId, id)
            },
            {
                "chat id",
                id => DraftProposal.ForChat(id, ProposalEntryTestData.CompanyId, ProposalEntryTestData.FileId)
            },
            {
                "chat company",
                id => DraftProposal.ForChat(ProposalEntryTestData.AttemptId, id, ProposalEntryTestData.FileId)
            },
            {
                "chat chat",
                id => DraftProposal.ForChat(ProposalEntryTestData.AttemptId, ProposalEntryTestData.CompanyId, id)
            },
        };
}
