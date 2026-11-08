using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class DeleteMachineTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteMachine_Always_HidesMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var machine = await _fixture.LoadMachineRowAsync(machineId);
        var get = () => _fixture.GetMachineAsync(tenant.CompanyId, machineId);

        machine.DeletedAt.Should().NotBeNull();
        machine.DeletedBy.Should().NotBeNull();
        await get.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteMachine_WithActiveMachine_StoresMachineDeletedOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDeleted>();
        var message = messages.Where(candidate => candidate.MachineId == machineId).ToList();

        message.Should().ContainSingle();
        message[index: 0].CompanyId.Should().Be(tenant.CompanyId);
    }

    [Fact]
    public async Task DeleteMachine_InOtherCompany_StoresNoOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>();
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDeleted>();

        messages.Should().NotContain(candidate => candidate.MachineId == machineId);
    }

    [Fact]
    public async Task DeleteMachine_Twice_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task DeleteMachine_InOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMachine_Always_SoftDeletesStateHistory()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber);
        var firstHistory = await _fixture.LoadStateHistoryAsync(machineId);
        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.InMaintenance,
            firstHistory[index: 0].StartedAt.AddDays(MachineTestData.EarlierStateDays));

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var history = await _fixture.LoadStateHistoryAsync(machineId);
        var otherHistory = await _fixture.LoadStateHistoryAsync(otherMachineId);

        history.Should().HaveCount(expected: 2).And.OnlyContain(row => row.DeletedAt != null);
        otherHistory.Should().ContainSingle().Which.DeletedAt.Should().BeNull();
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
}
