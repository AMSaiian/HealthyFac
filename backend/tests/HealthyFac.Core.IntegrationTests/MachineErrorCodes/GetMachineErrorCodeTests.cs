using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes;

public sealed class GetMachineErrorCodeTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetMachineErrorCode_WithValidIds_ReturnsValues()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code,
            MachineErrorCodeTestData.Description);

        var response = await _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        response.Id.Should().Be(errorCodeId);
        response.Code.Should().Be(MachineErrorCodeTestData.Code);
        response.Description.Should().Be(MachineErrorCodeTestData.Description);
        response.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetMachineErrorCode_ThroughOtherMachine_ThrowsNotFound()
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

        var act = () => _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrorCodeErrors.ErrorCodeNotFound);
    }

    [Fact]
    public async Task GetMachineErrorCode_OfDeletedErrorCode_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        await _fixture.RemoveMachineErrorCodeRowAsync(tenant, errorCodeId);

        var act = () => _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrorCodeErrors.ErrorCodeNotFound);
    }

    [Fact]
    public async Task GetMachineErrorCode_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task GetMachineErrorCode_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(otherTenant);
        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.GetMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            errorCodeId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }
}
