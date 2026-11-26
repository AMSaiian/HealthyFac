using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class DismissDefinitionDraftTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Dismiss_OfAPendingMetricDraft_DismissesIt()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        await _fixture.DismissDefinitionDraftAsync(companyId, draftId);

        var draft = await _fixture.LoadMetricDraftAsync(draftId);

        draft.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    [Fact]
    public async Task Dismiss_OfAnAwaitingStatusDraft_DismissesIt()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var draftId = await _fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);

        await _fixture.DismissDefinitionDraftAsync(companyId, draftId);

        var statuses = await _fixture.LoadStatusDraftsAsync();

        statuses.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Dismissed);
    }

    [Fact]
    public async Task Dismiss_OfAnAppliedDraft_ThrowsUnprocessableWithTheDraftNotPendingKey()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);

        var act = () => _fixture.DismissDefinitionDraftAsync(companyId, draftId);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(DefinitionDraftErrors.DraftNotPending);
    }

    [Fact]
    public async Task Dismiss_Twice_ThrowsUnprocessableForTheSecondCall()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.DismissDefinitionDraftAsync(companyId, draftId);

        var act = () => _fixture.DismissDefinitionDraftAsync(companyId, draftId);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(DefinitionDraftErrors.DraftNotPending);
    }

    [Fact]
    public async Task Dismiss_OfADraftOfAnotherCompanyOrAnUnknownId_ThrowsNotFound()
    {
        var otherCompanyId = Guid.CreateVersion7();
        var machineId = await _fixture.RegisterTenantMachineAsync(otherCompanyId);
        var foreignDraftId = await _fixture.StoreMetricDraftOnMachineAsync(otherCompanyId, machineId, isApplied: false);

        var foreignAct = () => _fixture.DismissDefinitionDraftAsync(DefinitionDraftTestData.CompanyId, foreignDraftId);

        var unknownAct = () => _fixture.DismissDefinitionDraftAsync(
            DefinitionDraftTestData.CompanyId, Guid.CreateVersion7());

        await foreignAct.Should().ThrowAsync<NotFoundException>();
        await unknownAct.Should().ThrowAsync<NotFoundException>();
    }
}
