using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Infrastructure.Drafts.Persistence;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftProposalConstraintTestData
{
    public const string SourceFieldsCheck = DraftProposalConfiguration.SourceFieldsCheckName;

    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01");

    private const string AnyId = "'0190f3a0-0000-7000-8000-000000000bbb'";

    public static TheoryData<string, Func<DraftProposal>> ValidProposals =>
        new()
        {
            { "ingest", Ingest },
            { "case", Case },
            { "chat", Chat },
        };

    public static TheoryData<string, Func<DraftProposal>, string, string> BrokenFields =>
        new()
        {
            { "ingest without file", Ingest, "file_id", "NULL" },
            { "ingest without queued time", Ingest, "queued_at", "NULL" },
            { "ingest with a case", Ingest, "case_id", AnyId },
            { "ingest with a chat", Ingest, "chat_id", AnyId },
            { "case without run", Case, "run_id", "NULL" },
            { "case with a file", Case, "file_id", AnyId },
            { "chat without message", Chat, "chat_message_id", "NULL" },
            { "chat with an attempt", Chat, "attempt_id", AnyId },
            { "chat with a run", Chat, "run_id", AnyId },
        };

    public static TheoryData<string, Func<DraftProposal>, string, string> DuplicateKeys =>
        new()
        {
            { "attempt", Ingest, "attempt_id", DraftIndexNames.ProposalAttempt },
            { "planner run", Case, "run_id", DraftIndexNames.ProposalRun },
            { "chat message", Chat, "chat_message_id", DraftIndexNames.ProposalChatMessage },
        };

    public static DraftProposal IngestProposal() => Ingest();

    public static DraftProposal ChatOf(Guid chatId) =>
        DraftProposal.ForChat(Guid.CreateVersion7(), CompanyId, chatId);

    private static DraftProposal Ingest() =>
        DraftProposal.ForIngest(
            Guid.CreateVersion7(),
            CompanyId,
            DraftTestData.FileId,
            DraftTestData.QueuedAt);

    private static DraftProposal Case() =>
        DraftProposal.ForCase(Guid.CreateVersion7(), CompanyId, Guid.CreateVersion7());

    private static DraftProposal Chat() =>
        DraftProposal.ForChat(Guid.CreateVersion7(), CompanyId, Guid.CreateVersion7());
}
