using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes;

public sealed class UpdateMachineErrorCodeTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateMachineErrorCode_WithNewValues_ChangesCodeAndDescription()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code,
            MachineErrorCodeTestData.Description);

        await _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId,
            MachineErrorCodeTestData.UpdatedCode,
            MachineErrorCodeTestData.UpdatedDescription);

        var response = await _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        response.Code.Should().Be(MachineErrorCodeTestData.UpdatedCode);
        response.Description.Should().Be(MachineErrorCodeTestData.UpdatedDescription);
        response.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMachineErrorCode_ToCodeOfOtherActiveErrorCode_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.OtherCode);

        var act = () => _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId,
            MachineErrorCodeTestData.CodeOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MachineErrorCodeErrors.CodeTaken);
    }

    [Fact]
    public async Task UpdateMachineErrorCode_ThroughOtherMachine_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateErrorCodeMachineAsync(
            tenant,
            MachineErrorCodeTestData.SecondMachineName,
            MachineErrorCodeTestData.SecondMachineSerialNumber);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            errorCodeId,
            MachineErrorCodeTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrorCodeErrors.ErrorCodeNotFound);
    }

    [Fact]
    public async Task UpdateMachineErrorCode_OfDeletedErrorCode_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        await _fixture.RemoveMachineErrorCodeRowAsync(tenant, errorCodeId);

        var act = () => _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId,
            MachineErrorCodeTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrorCodeErrors.ErrorCodeNotFound);
    }

    [Fact]
    public async Task UpdateMachineErrorCode_InForeignDepartment_ThrowsNotFoundAndKeepsRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            errorCodeId,
            MachineErrorCodeTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.Code.Should().Be(MachineErrorCodeTestData.Code);
    }

    [Fact]
    public async Task UpdateMachineErrorCode_ForMachineOfOtherCompany_ThrowsNotFoundAndKeepsRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(otherTenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.UpdateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId,
            MachineErrorCodeTestData.UpdatedCode);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.Code.Should().Be(MachineErrorCodeTestData.Code);
    }
}
