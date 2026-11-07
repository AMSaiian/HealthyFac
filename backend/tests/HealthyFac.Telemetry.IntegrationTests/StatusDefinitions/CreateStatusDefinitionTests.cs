using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

public sealed class CreateStatusDefinitionTests(TelemetryDatabaseFixture fixture)
    : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateStatusDefinition_WithValidRequest_StoresRowWithAuditFields()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.Description);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.MachineRegistrationId.Should().Be(machineId);
        row.Code.Should().Be(StatusDefinitionTestData.Code);
        row.Description.Should().Be(StatusDefinitionTestData.Description);
        row.CreatedAt.Should().NotBe(default(DateTime));
        row.CreatedBy.Should().NotBeEmpty();
        row.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateStatusDefinition_WithVisibleTextAtLimitInsideFormatting_StoresDescription()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.FormattedDescriptionAtVisibleLimit);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.Description.Should().Be(StatusDefinitionTestData.FormattedDescriptionAtVisibleLimit);
        row.Description!.Length.Should().BeGreaterThan(StatusDefinitionTestData.DescriptionLimit);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithDuplicateCode_ThrowsConflict()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(StatusDefinitionErrors.CodeTaken);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithCodeInOtherCase_ThrowsConflict()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.CodeOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(StatusDefinitionErrors.CodeTaken);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithCodeOfOtherMachine_Succeeds()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var firstMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var secondMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            firstMachineId,
            StatusDefinitionTestData.Code);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            secondMachineId,
            StatusDefinitionTestData.Code);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.MachineRegistrationId.Should().Be(secondMachineId);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithCodeOfDeletedStatusDefinition_Succeeds()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var firstId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);
        await _fixture.RemoveStatusDefinitionRowAsync(tenant, firstId);

        var secondId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.CodeOtherCase);

        var rows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, StatusDefinitionTestData.Code);

        rows.Select(row => row.Id).Should().BeEquivalentTo([firstId, secondId]);
        rows.Count(row => row.DeletedAt is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task CreateStatusDefinition_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task CreateStatusDefinition_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task CreateStatusDefinition_ForDeletedMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }
}
