using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts;

[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class DraftProposalTests
{
    [Fact]
    public void ForIngest_WithValidIds_SetsOnlyTheIngestFields()
    {
        var proposal = DraftProposal.ForIngest(
            ProposalEntryTestData.AttemptId,
            ProposalEntryTestData.CompanyId,
            ProposalEntryTestData.FileId);

        proposal.Source.Should().Be(DraftSource.Ingest);
        proposal.AttemptId.Should().Be(ProposalEntryTestData.AttemptId);
        proposal.FileId.Should().Be(ProposalEntryTestData.FileId);
        proposal.ChatId.Should().BeNull();
        proposal.ChatMessageId.Should().BeNull();
    }

    [Fact]
    public void ForChat_WithValidIds_SetsOnlyTheChatFields()
    {
        var proposal = DraftProposal.ForChat(
            ProposalEntryTestData.AttemptId,
            ProposalEntryTestData.CompanyId,
            ProposalEntryTestData.FileId);

        proposal.Source.Should().Be(DraftSource.Chat);
        proposal.ChatMessageId.Should().Be(ProposalEntryTestData.AttemptId);
        proposal.ChatId.Should().Be(ProposalEntryTestData.FileId);
        proposal.AttemptId.Should().BeNull();
        proposal.FileId.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(DraftProposalTestData.EmptyIdCases), MemberType = typeof(DraftProposalTestData))]
    public void Factory_WithEmptyId_Throws(string name, Func<Guid, DraftProposal> create)
    {
        var act = () => create(Guid.Empty);

        act.Should().Throw<InvalidOperationException>(name);
    }
}
