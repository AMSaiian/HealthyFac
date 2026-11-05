using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Machines;

public sealed class DeleteMachineTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteMachine_Always_HidesMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var machine = await _fixture.LoadMachineRowAsync(machineId);
        var get = () => _fixture.GetMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        machine.DeletedAt.Should().NotBeNull();
        machine.DeletedBy.Should().NotBeNull();
        await get.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteMachine_Twice_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task DeleteMachine_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, [tenant.SecondDepartmentId], machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMachine_WithErrorCodes_SoftDeletesAllItsErrorCodes()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        var firstId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        var secondId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.OtherCode);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var first = await _fixture.LoadMachineErrorCodeRowAsync(firstId);
        var second = await _fixture.LoadMachineErrorCodeRowAsync(secondId);

        first.DeletedAt.Should().NotBeNull();
        first.DeletedBy.Should().NotBeNull();
        second.DeletedAt.Should().NotBeNull();
        second.DeletedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMachine_WithErrorCodes_KeepsErrorCodesOfOtherMachineActive()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        var otherId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            MachineErrorCodeTestData.Code);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var other = await _fixture.LoadMachineErrorCodeRowAsync(otherId);

        other.DeletedAt.Should().BeNull();
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
}
