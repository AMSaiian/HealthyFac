using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class DraftProposalConstraintTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(DraftProposalConstraintTestData.ValidProposals), MemberType = typeof(DraftProposalConstraintTestData))]
    public async Task Save_WithTheFieldsOfItsSource_Succeeds(string caseName, Func<DraftProposal> create)
    {
        var proposal = create();

        await _fixture.SaveDraftProposalAsync(proposal);

        var stored = await _fixture.LoadDraftProposalsAsync();
        stored.Should().ContainSingle(candidate => candidate.Id == proposal.Id, caseName);
    }

    [Theory]
    [MemberData(
        nameof(DraftProposalConstraintTestData.BrokenFields), MemberType = typeof(DraftProposalConstraintTestData))]
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
            .WithMessage($"*{DraftProposalConstraintTestData.SourceFieldsCheck}*");
    }
}
