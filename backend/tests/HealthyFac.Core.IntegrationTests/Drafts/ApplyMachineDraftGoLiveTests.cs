using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Machines)]
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
    public async Task ApplyMachineDraft_OfAChatDraft_SendsTheAppliedMessageWithoutAFile()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddChatMachineDraftAsync(tenant.CompanyId);

        await _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machine = await _fixture.LoadSingleMachineAsync(tenant.CompanyId);
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDraftApplied>();

        var message = messages.Should().ContainSingle().Subject;
        message.DraftId.Should().Be(draftId);
        message.MachineId.Should().Be(machine.Id);
        message.FileId.Should().BeNull();
        message.QueuedAt.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMachineDraft_OfAnIngestDraft_SendsTheAppliedMessageWithTheFileAndTheQueueTime()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);

        await _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDraftApplied>();

        var message = messages.Should().ContainSingle().Subject;
        message.FileId.Should().Be(DraftTestData.FileId);
        message.QueuedAt.Should().Be(DraftTestData.QueuedAt);
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
