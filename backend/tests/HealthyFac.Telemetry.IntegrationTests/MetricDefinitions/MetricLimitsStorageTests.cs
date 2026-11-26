using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Intake;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions;

[Collection(TelemetryIntakeCollection.Name)]
[Feature(TestFeatures.SignalModel)]
public sealed class MetricLimitsStorageTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ChangeLimits_WithLimitsAndCounter_ReadsBackEqual()
    {
        var machineId = await _fixture.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        var metricId = await _fixture.AddMetricDefinitionAsync(machineId);
        var limits = new MetricLimits { WarningLow = 10, WarningHigh = 80, AlarmLow = 5, AlarmHigh = 90 };

        await _fixture.ChangeMetricLimitsAsync(metricId, limits, MetricBehavior.Counter);

        var metric = await _fixture.LoadMetricDefinitionRowAsync(metricId);

        metric.Limits.Should().Be(limits);
        metric.Behavior.Should().Be(MetricBehavior.Counter);
    }

    [Fact]
    public async Task LoadMetric_WithRowInsertedWithoutTheNewColumns_ReadsLevelAndNoLimits()
    {
        var machineId = await _fixture.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        var metricId = await _fixture.AddMetricDefinitionAsync(machineId);
        var oldStyleId = await _fixture.InsertMetricWithoutLimitColumnsAsync(metricId);

        var metric = await _fixture.LoadMetricDefinitionRowAsync(oldStyleId);

        metric.Limits.Should().Be(MetricLimits.None);
        metric.Behavior.Should().Be(MetricBehavior.Level);
    }
}
