using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
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
}
