using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.UnitTests.Drafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Drafts;

[Feature(TestFeatures.Drafts)]
public sealed class DraftProposalTests
{
    [Fact]
    public void ForIngest_WithValidIds_SetsOnlyTheIngestFields()
    {
        var proposal = DraftProposal.ForIngest(
            DraftTestData.AttemptId,
            DraftTestData.CompanyId,
            DraftTestData.FileId,
            DraftTestData.QueuedAt);

        proposal.Source.Should().Be(DraftSource.Ingest);
        proposal.AttemptId.Should().Be(DraftTestData.AttemptId);
        proposal.FileId.Should().Be(DraftTestData.FileId);
        proposal.QueuedAt.Should().Be(DraftTestData.QueuedAt);
        proposal.CaseId.Should().BeNull();
        proposal.RunId.Should().BeNull();
        proposal.ChatId.Should().BeNull();
        proposal.ChatMessageId.Should().BeNull();
    }

    [Fact]
    public void ForCase_WithValidIds_SetsOnlyTheCaseFields()
    {
        var proposal = DraftProposal.ForCase(DraftTestData.AttemptId, DraftTestData.CompanyId, DraftTestData.FileId);

        proposal.Source.Should().Be(DraftSource.Case);
        proposal.RunId.Should().Be(DraftTestData.AttemptId);
        proposal.CaseId.Should().Be(DraftTestData.FileId);
        proposal.AttemptId.Should().BeNull();
        proposal.FileId.Should().BeNull();
        proposal.QueuedAt.Should().BeNull();
        proposal.ChatId.Should().BeNull();
        proposal.ChatMessageId.Should().BeNull();
    }

    [Fact]
    public void ForChat_WithValidIds_SetsOnlyTheChatFields()
    {
        var proposal = DraftProposal.ForChat(DraftTestData.AttemptId, DraftTestData.CompanyId, DraftTestData.FileId);

        proposal.Source.Should().Be(DraftSource.Chat);
        proposal.ChatMessageId.Should().Be(DraftTestData.AttemptId);
        proposal.ChatId.Should().Be(DraftTestData.FileId);
        proposal.AttemptId.Should().BeNull();
        proposal.FileId.Should().BeNull();
        proposal.QueuedAt.Should().BeNull();
        proposal.CaseId.Should().BeNull();
        proposal.RunId.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(DraftProposalTestData.EmptyIdCases), MemberType = typeof(DraftProposalTestData))]
    public void Factory_WithEmptyId_Throws(string name, Func<Guid, DraftProposal> create)
    {
        var act = () => create(Guid.Empty);

        act.Should().Throw<InvalidOperationException>(name);
    }
}
