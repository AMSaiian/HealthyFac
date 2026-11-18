using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class StoreProposedDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Store_WithTwoEntries_StoresEveryChildAwaitingItsTarget()
    {
        var targetMachineId = Guid.CreateVersion7();
        var withTarget = ProposalBuilder.Machine(targetMachineId);
        var withoutTarget = ProposalBuilder.Machine();
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, withTarget, withoutTarget);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().HaveCount(expected: 2);
        statuses.Should().HaveCount(expected: 2);
        metrics.Concat<IDefinitionDraft>(statuses)
            .Should()
            .OnlyContain(draft => draft.State == DefinitionDraftState.AwaitingTarget);
        metrics.Single(draft => draft.ProposalEntryId == withTarget.DraftId)
            .TargetMachineId.Should()
            .Be(targetMachineId);
        metrics.Single(draft => draft.ProposalEntryId == withoutTarget.DraftId)
            .TargetMachineId.Should()
            .BeNull();
        entries.Select(entry => entry.Id).Should().BeEquivalentTo([withTarget.DraftId, withoutTarget.DraftId]);
        entries.Should().OnlyContain(entry => entry.State == ProposalEntryState.Awaiting);
    }

    [Fact]
    public async Task Store_Twice_StoresNothingNew()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());
        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().HaveCount(expected: 1);
        statuses.Should().HaveCount(expected: 1);
        entries.Should().HaveCount(expected: 1);
    }

    [Fact]
    public async Task Store_AfterASoftDelete_DoesNotStoreTheDraftAgain()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());
        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);
        var stored = await _fixture.LoadMetricDraftsAsync();
        await _fixture.SoftDeleteMetricDraftAsync(stored.Single().Id);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();

        metrics.Should().ContainSingle().Which.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Store_ForAnotherCompany_ThrowsAndStoresNothing()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());

        var act = () => _fixture.StoreProposedDefinitionDraftsAsync(Guid.CreateVersion7(), message);

        await act.Should().ThrowAsync<ForbiddenAccessException>();

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().BeEmpty();
        entries.Should().BeEmpty();
    }
}
