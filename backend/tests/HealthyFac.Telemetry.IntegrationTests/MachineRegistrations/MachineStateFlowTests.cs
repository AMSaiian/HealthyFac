using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Intake;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MachineRegistrations;

[Collection(TelemetryIntakeCollection.Name)]
public sealed class MachineStateFlowTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ChangeMachineState_WithStateMessage_StoresTheStateAndItsStart()
    {
        var machineId = await ArrangeRegistrationAsync();

        await _fixture.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                machineId,
                MachineStateValue.InMaintenance,
                MachineRegistrationTestData.StateStart));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.State.Should().Be(MachineRegistrationState.InMaintenance);
        registration.StateStartedAt.Should().Be(MachineRegistrationTestData.StateStart);
    }

    [Fact]
    public async Task ChangeMachineState_WithOlderStart_ChangesNothing()
    {
        var machineId = await ArrangeRegistrationAsync();
        await _fixture.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                machineId,
                MachineStateValue.InMaintenance,
                MachineRegistrationTestData.StateStart));

        await _fixture.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                machineId,
                MachineStateValue.OutOfService,
                MachineRegistrationTestData.StateStart.AddMinutes(value: -1)));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.State.Should().Be(MachineRegistrationState.InMaintenance);
        registration.StateStartedAt.Should().Be(MachineRegistrationTestData.StateStart);
    }

    [Fact]
    public async Task RegisterMachine_WithGoLive_StoresBothGoLiveValues()
    {
        var message = MachineRegistrationTestData.CreateMessage() with
        {
            ExpectedGoLiveOn = MachineRegistrationTestData.GoLiveDay,
            GoLiveStartsAt = MachineRegistrationTestData.GoLiveStart,
        };

        await _fixture.RegisterMachineAsync(message);

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.GoLiveDay);
        registration.GoLiveStartsAt.Should().Be(MachineRegistrationTestData.GoLiveStart);
    }

    [Fact]
    public async Task RegisterMachine_WithGoLiveOnRegistrationWithGoLive_KeepsTheStoredGoLive()
    {
        var message = MachineRegistrationTestData.CreateMessage() with
        {
            ExpectedGoLiveOn = MachineRegistrationTestData.GoLiveDay,
            GoLiveStartsAt = MachineRegistrationTestData.GoLiveStart,
        };
        await _fixture.RegisterMachineAsync(message);

        await _fixture.RegisterMachineAsync(
            message with
            {
                ExpectedGoLiveOn = MachineRegistrationTestData.OtherGoLiveDay,
                GoLiveStartsAt = MachineRegistrationTestData.OtherGoLiveStart,
            });

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.GoLiveDay);
    }

    [Fact]
    public async Task ChangeMachineGoLiveDate_WithNewerChange_StoresTheNewDay()
    {
        var machineId = await ArrangeRegistrationAsync();

        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                machineId,
                MachineRegistrationTestData.OtherGoLiveDay,
                MachineRegistrationTestData.OtherGoLiveStart,
                MachineRegistrationTestData.StateStart));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.OtherGoLiveDay);
        registration.GoLiveStartsAt.Should().Be(MachineRegistrationTestData.OtherGoLiveStart);
        registration.GoLiveChangedAt.Should().Be(MachineRegistrationTestData.StateStart);
    }

    [Fact]
    public async Task ChangeMachineGoLiveDate_WithOlderChange_ChangesNothing()
    {
        var machineId = await ArrangeRegistrationAsync();
        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                machineId,
                MachineRegistrationTestData.GoLiveDay,
                MachineRegistrationTestData.GoLiveStart,
                MachineRegistrationTestData.StateStart));

        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                machineId,
                MachineRegistrationTestData.OtherGoLiveDay,
                MachineRegistrationTestData.OtherGoLiveStart,
                MachineRegistrationTestData.StateStart.AddMinutes(value: -1)));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.GoLiveDay);
    }

    [Fact]
    public async Task ChangeMachineGoLiveDate_WithStoredDayThenOlderChangeWithOtherDay_KeepsTheDayAndTheNewerTime()
    {
        var message = MachineRegistrationTestData.CreateMessage() with
        {
            ExpectedGoLiveOn = MachineRegistrationTestData.GoLiveDay,
            GoLiveStartsAt = MachineRegistrationTestData.GoLiveStart,
        };
        await _fixture.RegisterMachineAsync(message);
        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                message.MachineId,
                MachineRegistrationTestData.GoLiveDay,
                MachineRegistrationTestData.GoLiveStart,
                MachineRegistrationTestData.StateStart.AddMinutes(value: 1)));

        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                message.MachineId,
                MachineRegistrationTestData.OtherGoLiveDay,
                MachineRegistrationTestData.OtherGoLiveStart,
                MachineRegistrationTestData.StateStart));

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.GoLiveDay);
        registration.GoLiveChangedAt.Should().Be(MachineRegistrationTestData.StateStart.AddMinutes(value: 1));
    }

    [Fact]
    public async Task ChangeMachineState_WithUnknownMachine_ThrowsNotFound()
    {
        var message = MachineRegistrationTestData.CreateStateMessage(
            Guid.CreateVersion7(),
            MachineStateValue.InMaintenance,
            MachineRegistrationTestData.StateStart);

        var act = () => _fixture.ChangeMachineStateAsync(message);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangeMachineGoLiveDate_WithUnknownMachine_ThrowsNotFound()
    {
        var message = MachineRegistrationTestData.CreateGoLiveMessage(
            Guid.CreateVersion7(),
            MachineRegistrationTestData.GoLiveDay,
            MachineRegistrationTestData.GoLiveStart,
            MachineRegistrationTestData.StateStart);

        var act = () => _fixture.ChangeMachineGoLiveDateAsync(message);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangeMachineState_WithDeletedRegistration_ChangesNothing()
    {
        var machineId = await ArrangeRegistrationAsync();
        await _fixture.DeleteMachineAsync(MachineRegistrationTestData.CompanyId, machineId);

        await _fixture.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                machineId,
                MachineStateValue.InMaintenance,
                MachineRegistrationTestData.StateStart));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.State.Should().Be(MachineRegistrationState.Operational);
        registration.StateStartedAt.Should().BeNull();
    }

    [Fact]
    public async Task ChangeMachineGoLiveDate_WithDeletedRegistration_ChangesNothing()
    {
        var machineId = await ArrangeRegistrationAsync();
        await _fixture.DeleteMachineAsync(MachineRegistrationTestData.CompanyId, machineId);

        await _fixture.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                machineId,
                MachineRegistrationTestData.GoLiveDay,
                MachineRegistrationTestData.GoLiveStart,
                MachineRegistrationTestData.StateStart));

        var registration = await _fixture.LoadRegistrationAsync(machineId);

        registration!.ExpectedGoLiveOn.Should().Be(MachineRegistrationTestData.InitialGoLiveDay);
    }

    private async Task<Guid> ArrangeRegistrationAsync()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);

        return message.MachineId;
    }
}
