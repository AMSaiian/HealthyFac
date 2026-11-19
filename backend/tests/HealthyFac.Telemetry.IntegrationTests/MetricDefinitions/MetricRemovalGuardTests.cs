using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions;

[Collection(TelemetryPhasesCollection.Name)]
public sealed class MetricRemovalGuardTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Remove_MetricThatAnActivePhaseConditionUses_Returns422AndKeepsTheMetric()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.AddPhaseUsingStateMetricAsync(arrangement, OperatingPhaseTestData.CuttingCode);

        var act = () => _fixture.RemoveMetricsAsync(arrangement.MachineId, arrangement.StateMetricId);

        var exception = await act.Should().ThrowAsync<UnprocessableException>();

        var rows = await _fixture.LoadMetricRowsAsync(arrangement.MachineId);
        exception.Which.Message.Should().Be(MetricDefinitionErrors.UsedByPhase);
        rows.Single(metric => metric.Id == arrangement.StateMetricId).DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Remove_MetricThatOnlyADeletedPhaseUses_Succeeds()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.AddPhaseUsingStateMetricAsync(arrangement, OperatingPhaseTestData.CuttingCode);
        await _fixture.DeletePhaseAsync(phaseId);

        await _fixture.RemoveMetricsAsync(arrangement.MachineId, arrangement.StateMetricId);

        var rows = await _fixture.LoadMetricRowsAsync(arrangement.MachineId);
        rows.Single(metric => metric.Id == arrangement.StateMetricId).DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Remove_MetricWithLimitRows_SoftDeletesItsRowsAndKeepsTheOtherRowsOfThePhase()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var secondMetricId = await _fixture.AddNumericMetricAsync(
            arrangement.MachineId, MetricRemovalTestData.SecondNumericKey);
        var phaseId = await _fixture.AddPhaseWithLimitsAsync(
            arrangement, OperatingPhaseTestData.CuttingCode, arrangement.MetricId, secondMetricId);

        await _fixture.RemoveMetricsAsync(arrangement.MachineId, arrangement.MetricId);

        var rows = await _fixture.LoadPhaseRowsAsync(phaseId);
        rows.Limits.Single(limit => limit.MetricDefinitionId == arrangement.MetricId).DeletedAt.Should().NotBeNull();
        rows.Limits.Single(limit => limit.MetricDefinitionId == secondMetricId).DeletedAt.Should().BeNull();
    }
}
