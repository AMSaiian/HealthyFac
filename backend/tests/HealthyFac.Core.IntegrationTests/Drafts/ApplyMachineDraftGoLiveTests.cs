using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class ApplyMachineDraftGoLiveTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyMachineDraft_CreateWithoutDate_SendsTodayAndItsStartInTheCompanyZone()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        await _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machine = await _fixture.LoadSingleMachineAsync(tenant.CompanyId);
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var message = messages.Single(candidate => candidate.MachineId == machine.Id);
        message.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.TodayInCompanyZone);
        message.GoLiveStartsAt.Should().Be(MachineGoLiveTestData.TodayInCompanyZoneStartsAt);
    }

    [Fact]
    public async Task ApplyMachineDraft_CreateWithLaterDate_SendsThatDayAndItsStart()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        await _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineGoLiveTestData.DayAfterToday);

        var machine = await _fixture.LoadSingleMachineAsync(tenant.CompanyId);
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var message = messages.Single(candidate => candidate.MachineId == machine.Id);
        message.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.DayAfterToday);
        message.GoLiveStartsAt.Should().Be(MachineGoLiveTestData.DayAfterTodayStartsAt);
    }
}
