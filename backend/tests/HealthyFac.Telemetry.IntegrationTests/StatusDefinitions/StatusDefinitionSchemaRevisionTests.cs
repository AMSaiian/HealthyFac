using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

public sealed class StatusDefinitionSchemaRevisionTests(TelemetryDatabaseFixture fixture)
    : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task StatusDefinitionChanges_Always_AddNoSchemaRevision()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, tenant.BothDepartmentIds, machineId, StatusDefinitionTestData.Code);
        await _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId, tenant.BothDepartmentIds, machineId, statusId, StatusDefinitionTestData.UpdatedCode);
        await _fixture.DeleteStatusDefinitionAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId, statusId);

        var revision = await _fixture.LoadMetricSchemaRevisionAsync(machineId);
        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);

        revision.Should().Be(expected: 0);
        revisions.Should().ContainSingle();
    }
}
