using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class UpdateMachineTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateMachine_WithNewValues_StoresThem()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber,
            MachineTestData.LinkDescription);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.Name.Should().Be(MachineTestData.OtherMachineName);
        machine.Description.Should().Be(MachineTestData.LinkDescription);
        machine.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMachine_WithValidRequest_KeepsDepartment()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.DepartmentId.Should().Be(tenant.FirstDepartmentId);
    }

    [Fact]
    public async Task UpdateMachine_ToTakenSerialNumber_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.OtherSerialNumber);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumberOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MachineErrors.SerialNumberTaken);
    }

    [Fact]
    public async Task UpdateMachine_KeepingOwnSerialNumber_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumberOtherCase);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateMachine_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task UpdateMachine_Always_KeepsDescriptionUnchanged()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.BulletListDescription);

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        machine.Description.Should().Be(MachineTestData.BulletListDescription);
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant, string serialNumber) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            serialNumber);
}
