using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class DraftProposalConstraintTestData
{
    public const string SourceFieldsCheck = DefinitionDraftCheckNames.ProposalSourceFields;

    private const string AnyId = "'0f1a2b3c-1000-7000-8000-000000000bbb'";

    public static TheoryData<string, Func<DraftProposal>> ValidProposals =>
        new()
        {
            { "ingest", Ingest },
            { "chat", Chat },
        };

    public static TheoryData<string, Func<DraftProposal>, string, string> BrokenFields =>
        new()
        {
            { "ingest without file", Ingest, "file_id", "NULL" },
            { "ingest without attempt", Ingest, "attempt_id", "NULL" },
            { "ingest with a chat", Ingest, "chat_id", AnyId },
            { "chat without message", Chat, "chat_message_id", "NULL" },
            { "chat without chat", Chat, "chat_id", "NULL" },
            { "chat with an attempt", Chat, "attempt_id", AnyId },
            { "chat with a file", Chat, "file_id", AnyId },
        };

    public static TheoryData<string, Func<DraftProposal>, string, string> DuplicateKeys =>
        new()
        {
            { "attempt", Ingest, "attempt_id", DefinitionDraftIndexNames.ProposalAttempt },
            { "chat message", Chat, "chat_message_id", DefinitionDraftIndexNames.ProposalChatMessage },
        };

    public static DraftProposal IngestProposal() => Ingest();

    public static DraftProposal ChatOf(Guid chatId) =>
        DraftProposal.ForChat(Guid.CreateVersion7(), DefinitionDraftTestData.CompanyId, chatId);

    private static DraftProposal Ingest() =>
        DraftProposal.ForIngest(
            Guid.CreateVersion7(),
            DefinitionDraftTestData.CompanyId,
            DefinitionDraftTestData.FileId);

    private static DraftProposal Chat() =>
        DraftProposal.ForChat(Guid.CreateVersion7(), DefinitionDraftTestData.CompanyId, Guid.CreateVersion7());
}
