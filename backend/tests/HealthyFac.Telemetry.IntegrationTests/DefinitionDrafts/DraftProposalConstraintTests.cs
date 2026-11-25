using EntityFramework.Exceptions.Common;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
[Feature(TestFeatures.Persistence)]
public sealed class DraftProposalConstraintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(DraftProposalConstraintTestData.ValidProposals),
        MemberType = typeof(DraftProposalConstraintTestData))]
    public async Task Save_WithTheFieldsOfItsSource_Succeeds(string caseName, Func<DraftProposal> create)
    {
        var proposal = create();

        await _fixture.SaveDraftProposalAsync(proposal);

        var stored = await _fixture.LoadDraftProposalsAsync();
        stored.Should().ContainSingle(candidate => candidate.Id == proposal.Id, caseName);
    }

    [Theory]
    [MemberData(
        nameof(DraftProposalConstraintTestData.BrokenFields),
        MemberType = typeof(DraftProposalConstraintTestData))]
    public async Task Update_WithAMissingFieldOrAFieldOfAnotherSource_FailsTheSourceFieldsCheck(
        string caseName,
        Func<DraftProposal> create,
        string column,
        string sqlValue
    )
    {
        var proposal = create();
        await _fixture.SaveDraftProposalAsync(proposal);

        var act = () => _fixture.SetDraftProposalColumnAsync(proposal.Id, column, sqlValue);

        await act.Should()
            .ThrowAsync<Exception>(caseName)
            .Where(exception => exception.ToString().Contains(DraftProposalConstraintTestData.SourceFieldsCheck));
    }

    [Theory]
    [MemberData(
        nameof(DraftProposalConstraintTestData.DuplicateKeys), MemberType = typeof(DraftProposalConstraintTestData))]
    public async Task Update_WithTheKeyOfAnotherProposal_FailsTheUniqueIndex(
        string caseName,
        Func<DraftProposal> create,
        string column,
        string indexName
    )
    {
        var first = create();
        var second = create();
        await _fixture.SaveDraftProposalAsync(first);
        await _fixture.SaveDraftProposalAsync(second);

        var act = () => _fixture.SetDraftProposalColumnAsync(
            second.Id,
            column,
            $"(SELECT {column} FROM draft_proposals WHERE id = '{first.Id}')");

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>(caseName);

        exception.Which.ConstraintName.Should().Be(indexName, caseName);
    }

    [Fact]
    public async Task Save_WithTwoChatProposalsOfOneChat_Succeeds()
    {
        var chatId = Guid.CreateVersion7();

        await _fixture.SaveDraftProposalAsync(DraftProposalConstraintTestData.ChatOf(chatId));
        await _fixture.SaveDraftProposalAsync(DraftProposalConstraintTestData.ChatOf(chatId));

        var stored = await _fixture.LoadDraftProposalsAsync();
        stored.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Update_WithTheAttemptOfASoftDeletedProposal_Succeeds()
    {
        var deleted = DraftProposalConstraintTestData.IngestProposal();
        var live = DraftProposalConstraintTestData.IngestProposal();
        await _fixture.SaveDraftProposalAsync(deleted);
        await _fixture.SaveDraftProposalAsync(live);
        await _fixture.SetDraftProposalColumnAsync(deleted.Id, column: "deleted_at", sqlValue: "now()");

        var act = () => _fixture.SetDraftProposalColumnAsync(
            live.Id,
            column: "attempt_id",
            $"(SELECT attempt_id FROM draft_proposals WHERE id = '{deleted.Id}')");

        await act.Should().NotThrowAsync();
    }
}
