using HealthyFac.Core.Domain.Drafts.Proposals;

namespace HealthyFac.Core.UnitTests.Drafts.Data;

public static class DraftProposalTestData
{
    public static TheoryData<string, Func<Guid, DraftProposal>> EmptyIdCases =>
        new()
        {
            {
                "ingest id",
                id => DraftProposal.ForIngest(id, DraftTestData.CompanyId, DraftTestData.FileId, DraftTestData.QueuedAt)
            },
            {
                "ingest company",
                id => DraftProposal.ForIngest(DraftTestData.AttemptId, id, DraftTestData.FileId, DraftTestData.QueuedAt)
            },
            {
                "ingest file",
                id => DraftProposal.ForIngest(
                    DraftTestData.AttemptId,
                    DraftTestData.CompanyId,
                    id,
                    DraftTestData.QueuedAt)
            },
            { "case id", id => DraftProposal.ForCase(id, DraftTestData.CompanyId, DraftTestData.FileId) },
            { "case company", id => DraftProposal.ForCase(DraftTestData.AttemptId, id, DraftTestData.FileId) },
            { "case case", id => DraftProposal.ForCase(DraftTestData.AttemptId, DraftTestData.CompanyId, id) },
            { "chat id", id => DraftProposal.ForChat(id, DraftTestData.CompanyId, DraftTestData.FileId) },
            { "chat company", id => DraftProposal.ForChat(DraftTestData.AttemptId, id, DraftTestData.FileId) },
            { "chat chat", id => DraftProposal.ForChat(DraftTestData.AttemptId, DraftTestData.CompanyId, id) },
        };
}
