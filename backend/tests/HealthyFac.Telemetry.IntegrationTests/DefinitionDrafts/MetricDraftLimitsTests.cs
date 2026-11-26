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
public sealed class MetricDraftLimitsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Store_WithLimitsAndBehavior_StoresThemOnTheMetricDraft()
    {
        var message = ProposalBuilder.Message(
            DefinitionDraftTestData.CompanyId,
            ProposalBuilder.Machine() with { Metrics = [MetricDraftLimitsTestData.ProposedMetricWithLimits()] });

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var drafts = await _fixture.LoadMetricDraftsAsync();
        drafts.Should().ContainSingle().Which.Should().BeEquivalentTo(MetricDraftLimitsTestData.StoredLimits);
    }

    [Fact]
    public async Task Store_WithoutLimitsAndBehavior_StoresNullOnTheMetricDraft()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var drafts = await _fixture.LoadMetricDraftsAsync();
        drafts.Should().ContainSingle().Which.Should().BeEquivalentTo(MetricDraftLimitsTestData.NoLimits);
    }

    [Fact]
    public async Task ApplyMetrics_WithADraftWithLimits_StoresTheLimitsOnTheDefinitionAndAppliesTheDraft()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var draftId = await _fixture.StoreMetricDraftOnMachineAsync(tenant.CompanyId, machineId, isApplied: false);

        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric() with { DraftId = draftId, AlarmHigh = 90, WarningHigh = 80 }]);

        var rows = await _fixture.LoadMetricRowsAsync(machineId);
        var drafts = await _fixture.LoadMetricDraftsAsync();
        rows.Single().Limits.Should().BeEquivalentTo(new { WarningHigh = 80.0, AlarmHigh = 90.0 });
        drafts.Single(draft => draft.Id == draftId).State.Should().Be(DefinitionDraftState.Applied);
    }
}
