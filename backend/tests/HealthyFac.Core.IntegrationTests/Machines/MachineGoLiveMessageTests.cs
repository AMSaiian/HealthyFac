using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class MachineGoLiveMessageTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateMachine_WithGoLiveDay_SendsTheDayAndItsStartInTheCompanyZone()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineGoLiveTestData.TodayInCompanyZone);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var message = messages.Single(candidate => candidate.MachineId == machineId);

        message.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.TodayInCompanyZone);
        message.GoLiveStartsAt.Should().Be(MachineGoLiveTestData.TodayInCompanyZoneStartsAt);
    }

    [Fact]
    public async Task UpdateMachine_WithNewGoLiveDay_StoresOneGoLiveDateChangedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
        _fixture.Clock.Freeze(MachineGoLiveTestData.LaterNow);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineTestData.SecondGoLiveOn);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineGoLiveDateChanged>();
        var changed = messages.Where(candidate => candidate.MachineId == machineId).ToList();

        changed.Should().ContainSingle();
        changed[index: 0].CompanyId.Should().Be(tenant.CompanyId);
        changed[index: 0].ExpectedGoLiveOn.Should().Be(MachineTestData.SecondGoLiveOn);
        changed[index: 0].GoLiveStartsAt.Should().Be(MachineTestData.SecondGoLiveStartsAt);
        changed[index: 0].ChangedAt.Should().Be(MachineGoLiveTestData.LaterNow.UtcDateTime);
    }

    [Fact]
    public async Task UpdateMachine_WithSameGoLiveDay_StoresNoGoLiveDateChangedMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumber);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineGoLiveDateChanged>();

        messages.Where(candidate => candidate.MachineId == machineId).Should().BeEmpty();
    }
}
