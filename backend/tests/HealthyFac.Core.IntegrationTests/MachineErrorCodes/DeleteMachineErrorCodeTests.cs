using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes;

public sealed class DeleteMachineErrorCodeTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteMachineErrorCode_Always_HidesErrorCode()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        await _fixture.DeleteMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);
        var get = () => _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        row.DeletedAt.Should().NotBeNull();
        row.DeletedBy.Should().NotBeNull();
        await get.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteMachineErrorCode_Twice_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        await _fixture.DeleteMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        var act = () => _fixture.DeleteMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrorCodeErrors.ErrorCodeNotFound);
    }

    [Fact]
    public async Task DeleteMachineErrorCode_InForeignDepartment_ThrowsNotFoundAndKeepsRowActive()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.DeleteMachineErrorCodeAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMachineErrorCode_ForMachineOfOtherCompany_ThrowsNotFoundAndKeepsRowActive()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(otherTenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.DeleteMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.DeletedAt.Should().BeNull();
    }
}
