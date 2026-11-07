using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

public sealed class GetStatusDefinitionTests(TelemetryDatabaseFixture fixture) : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetStatusDefinition_ThroughOtherMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.GetStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(StatusDefinitionErrors.StatusDefinitionNotFound);
    }

    [Fact]
    public async Task GetStatusDefinition_OfDeletedStatusDefinition_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);
        await _fixture.RemoveStatusDefinitionRowAsync(tenant, statusId);

        var act = () => _fixture.GetStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(StatusDefinitionErrors.StatusDefinitionNotFound);
    }

    [Fact]
    public async Task GetStatusDefinition_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.GetStatusDefinitionAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task GetStatusDefinition_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.GetStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }
}
