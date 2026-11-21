using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class MachineHintTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ChangeMachineState_ToAnotherState_RecordsTheMachinesHint()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeAsync(tenant);
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.Machines));
    }

    [Fact]
    public async Task ChangeMachineState_ToTheSameState_RecordsNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeAsync(tenant);
        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMachine_WithANewState_RecordsTheMachinesHint()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeAsync(tenant);
        _fixture.RealtimeQueue.Clear();

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.LinkDescription,
            state: MachineState.InMaintenance);

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.Machines));
    }

    [Fact]
    public async Task UpdateMachine_WithTheSameState_RecordsNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeAsync(tenant);
        _fixture.RealtimeQueue.Clear();

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    private async Task<Guid> ArrangeAsync(MachineTenant tenant)
    {
        _fixture.Clock.Freeze(MachineStateChangeTestData.CreatedNow);
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);
        _fixture.Clock.Freeze(MachineStateChangeTestData.ChangedNow);

        return machineId;
    }
}
