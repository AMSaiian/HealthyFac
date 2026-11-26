using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Machines)]
public sealed class MachineGoLiveTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateMachine_WithTodayInCompanyZone_StoresDate()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineGoLiveTestData.TodayInCompanyZone);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.TodayInCompanyZone);
    }

    [Fact]
    public async Task CreateMachine_WithDayBeforeTodayInCompanyZone_FailsOnExpectedGoLiveOn()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineGoLiveTestData.DayBeforeToday);

        var exception = await act.Should().ThrowAsync<AppValidationException>();

        exception.Which.Errors.Should()
            .ContainSingle(error =>
                error.PropertyName == MachineGoLiveTestData.ExpectedGoLiveOnKey);
    }

    [Fact]
    public async Task UpdateMachine_WithCreationDayAfterTodayMoved_StoresDate()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineCreatedAsync(tenant.CompanyId);
        _fixture.Clock.Freeze(MachineGoLiveTestData.LaterNow);

        await _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineGoLiveTestData.TodayInCompanyZone);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.TodayInCompanyZone);
    }

    [Fact]
    public async Task UpdateMachine_WithDayBeforeCreation_FailsOnExpectedGoLiveOn()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineCreatedAsync(tenant.CompanyId);
        _fixture.Clock.Freeze(MachineGoLiveTestData.LaterNow);

        var act = () => _fixture.UpdateMachineAsync(
            tenant.CompanyId,
            machineId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: MachineGoLiveTestData.DayBeforeToday);

        var exception = await act.Should().ThrowAsync<AppValidationException>();

        exception.Which.Errors.Should()
            .ContainSingle(error =>
                error.PropertyName == MachineGoLiveTestData.ExpectedGoLiveOnKey);
    }

    [Fact]
    public async Task ApplyMachineDraft_CreateWithoutDate_StoresTodayInCompanyZone()
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

        machine.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.TodayInCompanyZone);
    }

    [Fact]
    public async Task ApplyMachineDraft_CreateWithLaterDate_StoresIt()
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

        machine.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.DayAfterToday);
    }

    [Fact]
    public async Task ApplyMachineDraft_CreateWithDayBeforeToday_FailsOnExpectedGoLiveOn()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        var act = () => _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineGoLiveTestData.DayBeforeToday);

        var exception = await act.Should().ThrowAsync<AppValidationException>();

        exception.Which.Errors.Should()
            .ContainSingle(error =>
                error.PropertyName == MachineGoLiveTestData.ExpectedGoLiveOnKey);
    }

    [Fact]
    public async Task ApplyMachineDraft_UpdateWithoutDate_KeepsStoredDate()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineCreatedAsync(tenant.CompanyId, MachineGoLiveTestData.DayAfterToday);
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId);
        var stored = await _fixture.LoadMachineRowAsync(machineId);

        await _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumber,
            rowVersion: stored.RowVersion);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.ExpectedGoLiveOn.Should().Be(MachineGoLiveTestData.DayAfterToday);
        machine.Name.Should().Be(MachineTestData.OtherMachineName);
    }

    [Fact]
    public async Task ApplyMachineDraft_UpdateWithDate_FailsOnExpectedGoLiveOn()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineCreatedAsync(tenant.CompanyId);
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId);
        var stored = await _fixture.LoadMachineRowAsync(machineId);

        var act = () => _fixture.ApplyDraftAsync(
            tenant.CompanyId,
            draftId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumber,
            MachineGoLiveTestData.DayAfterToday,
            stored.RowVersion);

        var exception = await act.Should().ThrowAsync<AppValidationException>();

        exception.Which.Errors.Should()
            .ContainSingle(error =>
                error.PropertyName == MachineGoLiveTestData.ExpectedGoLiveOnKey);
    }

    private async Task<Guid> ArrangeMachineCreatedAsync(Guid companyId, DateOnly? expectedGoLiveOn = null)
    {
        _fixture.Clock.Freeze(MachineGoLiveTestData.CreatedNow);

        return await _fixture.CreateMachineAsync(
            companyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            expectedGoLiveOn: expectedGoLiveOn ?? MachineGoLiveTestData.TodayInCompanyZone);
    }
}
