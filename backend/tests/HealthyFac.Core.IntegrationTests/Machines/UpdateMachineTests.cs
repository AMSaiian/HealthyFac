using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;
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
    public async Task UpdateMachine_WithNewName_StoresOneMachineRenamedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId, machineId, MachineTestData.OtherMachineName, MachineTestData.SerialNumber);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineRenamed>();
        var renamed = messages.Where(candidate => candidate.MachineId == machineId).ToList();

        renamed.Should().ContainSingle();
        renamed[index: 0].Name.Should().Be(MachineTestData.OtherMachineName);
        renamed[index: 0].CompanyId.Should().Be(tenant.CompanyId);
    }

    [Fact]
    public async Task UpdateMachine_WithDescriptionOnlyChange_StoresNoMachineRenamedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineRenamed>();

        messages.Where(candidate => candidate.MachineId == machineId).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMachine_ToTakenSerialNumber_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);
        var machineId = await ArrangeMachineAsync(tenant, MachineTestData.OtherSerialNumber);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
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
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumberOtherCase);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateMachine_InOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant, MachineTestData.SerialNumber);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
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
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.BulletListDescription);

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, machineId);

        machine.Description.Should().Be(MachineTestData.BulletListDescription);
    }

    [Fact]
    public async Task UpdateMachine_WhenTwoUpdatesOverlap_OneSucceedsAndOneConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber);
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.UpdateMachineAsync(
                tenant.CompanyId, machineId, MachineTestData.MachineName, MachineTestData.SerialNumber,
                description: "First")),
            race.RunAsync(() => _fixture.UpdateMachineAsync(
                tenant.CompanyId, machineId, MachineTestData.MachineName, MachineTestData.SerialNumber,
                description: "Second")));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(MachineErrors.MachineChanged);
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant, string serialNumber) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            serialNumber);
}
