using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

public sealed class UpdateStatusDefinitionTests(TelemetryDatabaseFixture fixture)
    : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateStatusDefinition_ToCodeOfOtherActiveStatusDefinition_ThrowsConflict()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.OtherCode);

        var act = () => _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            statusId,
            StatusDefinitionTestData.CodeOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(StatusDefinitionErrors.CodeTaken);
    }

    [Fact]
    public async Task UpdateStatusDefinition_ThroughOtherMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            statusId,
            StatusDefinitionTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(StatusDefinitionErrors.StatusDefinitionNotFound);
    }

    [Fact]
    public async Task UpdateStatusDefinition_OfDeletedStatusDefinition_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);
        await _fixture.RemoveStatusDefinitionRowAsync(tenant, statusId);

        var act = () => _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            statusId,
            StatusDefinitionTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(StatusDefinitionErrors.StatusDefinitionNotFound);
    }

    [Fact]
    public async Task UpdateStatusDefinition_InForeignDepartment_ThrowsNotFoundAndKeepsRow()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            statusId,
            StatusDefinitionTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.Code.Should().Be(StatusDefinitionTestData.Code);
    }

    [Fact]
    public async Task UpdateStatusDefinition_ForMachineOfOtherCompany_ThrowsNotFoundAndKeepsRow()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.UpdateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            statusId,
            StatusDefinitionTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.Code.Should().Be(StatusDefinitionTestData.Code);
    }
}
