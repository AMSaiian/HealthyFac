using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class ChangeMachineStateTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(MachineStateChangeTestData.StatesOtherThanOperational),
        MemberType = typeof(MachineStateChangeTestData))]
    public async Task ChangeMachineState_ToOtherState_AddsOneRowThatStartsNow(MachineState state)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, state);

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should().HaveCount(expected: 2);
        history[index: 1].State.Should().Be(state);
        history[index: 1].StartedAt.Should().Be(MachineStateChangeTestData.ChangedNow.UtcDateTime);
    }

    [Fact]
    public async Task ChangeMachineState_ToOtherState_KeepsTheOtherFieldsAndStoresNoRenameMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, machineId);
        var renamed = await _fixture.ReadOutboxMessagesAsync<MachineRenamed>();

        machine.Name.Should().Be(MachineTestData.MachineName);
        machine.SerialNumber.Should().Be(MachineTestData.SerialNumber);
        machine.Description.Should().Be(MachineTestData.LinkDescription);
        machine.ExpectedGoLiveOn.Should().Be(MachineTestData.FutureGoLiveOn);
        machine.State.Should().Be(MachineState.InMaintenance);
        renamed.Where(message => message.MachineId == machineId).Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeMachineState_ToOtherState_SetsUpdatedAtAndUpdatedBy()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(
            tenant.CompanyId, machineId, MachineState.InMaintenance, MachineStateChangeTestData.ActorId);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.UpdatedAt.Should().Be(MachineStateChangeTestData.ChangedNow.UtcDateTime);
        machine.UpdatedBy.Should().Be(MachineStateChangeTestData.ActorId);
    }

    [Fact]
    public async Task ChangeMachineState_ToCurrentState_AddsNoRowAndKeepsUpdatedAt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.Operational);

        var history = await _fixture.LoadStateHistoryAsync(machineId);
        var machine = await _fixture.LoadMachineRowAsync(machineId);

        history.Should().ContainSingle();
        machine.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ChangeMachineState_ToCurrentState_ChangesNothingAfterAnEarlierChange()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.OutOfService);
        _fixture.Clock.Freeze(MachineStateChangeTestData.ChangedNow.AddDays(days: 1));

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.OutOfService);

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task ChangeMachineState_WithUnknownId_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.ChangeMachineStateAsync(
            tenant.CompanyId, Guid.CreateVersion7(), MachineState.InMaintenance);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task ChangeMachineState_InOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant);

        var act = () => _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task ChangeMachineState_OfDeletedMachine_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task ChangeMachineState_WhenTwoChangesOverlap_OneSucceedsAndOneConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber);
        _fixture.Clock.StepFrom(DateTimeOffset.UtcNow.AddMinutes(minutes: 1), TimeSpan.FromSeconds(seconds: 1));
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ChangeMachineStateAsync(
                tenant.CompanyId, machineId, MachineState.InMaintenance)),
            race.RunAsync(() => _fixture.ChangeMachineStateAsync(
                tenant.CompanyId, machineId, MachineState.OutOfService)));

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(MachineErrors.MachineChanged);
        history.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task ChangeMachineState_WhenTwoChangesHaveTheSameInstant_OneSucceedsAndOneConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber);
        _fixture.Clock.Freeze(DateTimeOffset.UtcNow.AddMinutes(minutes: 1));
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ChangeMachineStateAsync(
                tenant.CompanyId, machineId, MachineState.InMaintenance)),
            race.RunAsync(() => _fixture.ChangeMachineStateAsync(
                tenant.CompanyId, machineId, MachineState.OutOfService)));

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(MachineErrors.MachineChanged);
        history.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task ChangeMachineState_ToOtherState_StoresOneStateChangedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.InMaintenance);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineStateChanged>();
        var changed = messages.Where(message => message.MachineId == machineId).ToList();

        changed.Should().ContainSingle();
        changed[index: 0].CompanyId.Should().Be(tenant.CompanyId);
        changed[index: 0].State.Should().Be(MachineStateValue.InMaintenance);
        changed[index: 0].StartedAt.Should().Be(MachineStateChangeTestData.ChangedNow.UtcDateTime);
    }

    [Fact]
    public async Task ChangeMachineState_ToCurrentState_StoresNoStateChangedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.ChangeMachineStateAsync(tenant.CompanyId, machineId, MachineState.Operational);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineStateChanged>();

        messages.Where(message => message.MachineId == machineId).Should().BeEmpty();
    }

    private async Task<Guid> ArrangeMachineAsync(MachineTenant tenant)
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
