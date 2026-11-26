using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class MarkDraftOnApplyTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyMetrics_WithACreatedAndAnUpdatedDraft_StoresTheDefinitionsAndAppliesBothDrafts()
    {
        var (tenant, machineId) = await ArrangeAsync();

        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewBoolean()]);

        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();

        var createdDraftId =
            await _fixture.StoreMetricDraftOnMachineAsync(tenant.CompanyId, machineId, isApplied: false);

        var updatedDraftId =
            await _fixture.StoreMetricDraftOnMachineAsync(tenant.CompanyId, machineId, isApplied: false);

        var response = await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            [MetricDefinitionTestData.NewNumeric() with { DraftId = createdDraftId }],
            [MetricDefinitionTestData.Update(existing.Id, name: "Renamed") with { DraftId = updatedDraftId }]);

        var rows = await _fixture.LoadMetricRowsAsync(machineId);
        var drafts = await _fixture.LoadMetricDraftsAsync();

        response.Revision.Should().Be(expected: 2);
        rows.Should().HaveCount(expected: 2);

        drafts.Where(draft => draft.Id == createdDraftId || draft.Id == updatedDraftId)
            .Select(draft => draft.State)
            .Should()
            .OnlyContain(state => state == DefinitionDraftState.Applied);
    }

    [Fact]
    public async Task ApplyMetrics_WithAnEqualUpdatedDraftOnly_AppliesTheDraftAndKeepsTheRevision()
    {
        var (tenant, machineId) = await ArrangeAsync();

        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewBoolean()]);

        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(tenant.CompanyId, machineId, isApplied: false);

        var response = await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            updated: [MetricDefinitionTestData.Update(existing.Id, existing.Name) with { DraftId = draftId }]);

        var drafts = await _fixture.LoadMetricDraftsAsync();
        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);

        response.Revision.Should().Be(expected: 1);
        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Applied);
        revisions.Select(revision => revision.Revision).Should().Equal(0, 1);
    }

    [Fact]
    public async Task CreateStatus_WithADraft_AppliesTheDraft()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);

        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode, draftId: draftId);

        var drafts = await _fixture.LoadStatusDraftsAsync();

        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Applied);
    }

    [Fact]
    public async Task UpdateStatus_WithADraft_AppliesTheDraft()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode);

        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);

        await _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId, machineId, statusId, MarkDraftOnApplyTestData.StatusCode, draftId: draftId);

        var drafts = await _fixture.LoadStatusDraftsAsync();

        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Applied);
    }

    [Fact]
    public async Task ApplyStatuses_WithADraft_AppliesTheDraft()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);

        await _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [new StatusDefinitionChange { Code = MarkDraftOnApplyTestData.StatusCode, DraftId = draftId }]);

        var drafts = await _fixture.LoadStatusDraftsAsync();

        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Applied);
    }

    [Fact]
    public async Task CreateStatus_WithAnUnknownDraft_ThrowsValidationAtTheDraftPathAndStoresNothing()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode, draftId: Guid.CreateVersion7());

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        var rows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, MarkDraftOnApplyTestData.StatusCode);

        exception.Errors.Should()
            .ContainSingle(error => error.PropertyName == "draftId"
                && error.ErrorMessage == DefinitionDraftErrors.DraftNotUsable);

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateStatus_WithADraftOfAnotherMachine_ThrowsValidationAndKeepsTheDraftPending()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var otherMachineId = await _fixture.RegisterTenantMachineAsync(tenant.CompanyId);
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, otherMachineId);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode, draftId: draftId);

        await act.Should().ThrowAsync<AppValidationException>();
        var drafts = await _fixture.LoadStatusDraftsAsync();

        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task CreateStatus_WithADraftOfAnotherCompany_ThrowsValidation()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var otherCompanyId = Guid.CreateVersion7();
        var otherMachineId = await _fixture.RegisterTenantMachineAsync(otherCompanyId);
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(otherCompanyId, otherMachineId);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode, draftId: draftId);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task ApplyMetrics_WithAStatusDraft_ThrowsValidationAtTheItemPath()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var statusDraftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric() with { DraftId = statusDraftId }]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;

        exception.Errors.Should().ContainSingle(error => error.PropertyName == "created[0].draftId");
    }

    [Fact]
    public async Task ApplyMetrics_WithTheSameDraftTwice_ThrowsValidationAndStoresNothing()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(tenant.CompanyId, machineId, isApplied: false);

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [
                MetricDefinitionTestData.NewNumeric() with { DraftId = draftId },
                MetricDefinitionTestData.NewBoolean() with { DraftId = draftId },
            ]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        var rows = await _fixture.LoadMetricRowsAsync(machineId);

        exception.Errors.Should().ContainSingle(error => error.PropertyName == "created[1].draftId");
        rows.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(MarkDraftOnApplyTestData.NotPendingDrafts), MemberType = typeof(MarkDraftOnApplyTestData))]
    public async Task Apply_WithADraftThatIsNotPending_ThrowsUnprocessableWithTheDraftNotPendingKey(
        string caseName,
        Func<TelemetryDatabaseFixture, Guid, Guid, Task> applyWithDraft
    )
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => applyWithDraft(_fixture, tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(DefinitionDraftErrors.DraftNotPending);
        (await _fixture.LoadMetricRowsAsync(machineId)).Should().BeEmpty(caseName);
    }

    [Theory]
    [MemberData(nameof(MarkDraftOnApplyTestData.ParallelWrites), MemberType = typeof(MarkDraftOnApplyTestData))]
    public async Task Write_OfTheSameDraftInParallel_AppliesOnceAndTheOtherCallConflicts(
        string caseName,
        Func<TelemetryDatabaseFixture, Guid, Guid, Guid, int, IReadOnlyList<Guid>, Task> writeWithDraft
    )
    {
        var (tenant, machineId) = await ArrangeAsync();
        var firstStatusId = await _fixture.CreateStatusDefinitionAsync(tenant.CompanyId, machineId, code: "U-0");
        var secondStatusId = await _fixture.CreateStatusDefinitionAsync(tenant.CompanyId, machineId, code: "U-1");
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);
        using var race = _fixture.SaveGate.StartRace(participantCount: 2);

        var failures = await Task.WhenAll(
            race.RunAsync(() => writeWithDraft(
                _fixture, tenant.CompanyId, machineId, draftId, arg5: 0, [firstStatusId, secondStatusId])),
            race.RunAsync(() => writeWithDraft(
                _fixture, tenant.CompanyId, machineId, draftId, arg5: 1, [firstStatusId, secondStatusId])));

        race.SaveCounts.Should().Equal(1, 1);

        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DefinitionDraftErrors.DraftChanged, caseName);
    }

    [Fact]
    public async Task CreateStatus_InParallelWithADismissOfTheDraft_ConflictsAndStoresADefinitionOnlyForAnAppliedDraft()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var draftId = await _fixture.StorePendingStatusDraftOnMachineAsync(tenant.CompanyId, machineId);
        using var race = _fixture.SaveGate.StartRace(participantCount: 2);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.CreateStatusDefinitionAsync(
                tenant.CompanyId, machineId, MarkDraftOnApplyTestData.StatusCode, draftId: draftId)),
            race.RunAsync(() => _fixture.DismissDefinitionDraftAsync(tenant.CompanyId, draftId)));

        var definitions = await _fixture.LoadStatusDefinitionRowsAsync(machineId, MarkDraftOnApplyTestData.StatusCode);
        var drafts = await _fixture.LoadStatusDraftsAsync();

        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        definitions.Count.Should().Be(drafts.Count(draft => draft.State == DefinitionDraftState.Applied));
    }

    private async Task<(MachineTenant Tenant, Guid MachineId)> ArrangeAsync()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        return (tenant, machineId);
    }
}
