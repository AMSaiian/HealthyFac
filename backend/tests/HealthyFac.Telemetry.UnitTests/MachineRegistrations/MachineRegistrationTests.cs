using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.UnitTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.UnitTests.MachineRegistrations;

public sealed class MachineRegistrationTests
{
    [Theory]
    [MemberData(nameof(MachineRegistrationTestData.StateStarts), MemberType = typeof(MachineRegistrationTestData))]
    public void ChangeState_WithStartAgainstStoredStart_ChangesOnlyForANewerStart(
        string caseName,
        DateTime startedAt,
        bool isChanged
    )
    {
        var registration = MachineRegistrationTestData.Create();
        registration.ChangeState(MachineRegistrationState.InMaintenance, MachineRegistrationTestData.StoredTime);

        var result = registration.ChangeState(MachineRegistrationState.OutOfService, startedAt);

        result.Should().Be(isChanged, caseName);
        registration.State.Should()
            .Be(
                isChanged ? MachineRegistrationState.OutOfService : MachineRegistrationState.InMaintenance);
        registration.StateStartedAt.Should().Be(isChanged ? startedAt : MachineRegistrationTestData.StoredTime);
    }

    [Fact]
    public void ChangeState_WithNoStoredState_StoresTheState()
    {
        var registration = MachineRegistrationTestData.Create();

        var result = registration.ChangeState(
            MachineRegistrationState.OutOfService,
            MachineRegistrationTestData.StoredTime);

        result.Should().BeTrue();
        registration.State.Should().Be(MachineRegistrationState.OutOfService);
    }

    [Theory]
    [MemberData(nameof(MachineRegistrationTestData.GoLiveChanges), MemberType = typeof(MachineRegistrationTestData))]
    public void ChangeGoLive_WithChangeAgainstStoredGoLive_AppliesOnlyANewerChange(
        string caseName,
        DateTime changedAt,
        DateOnly day,
        bool isChanged
    )
    {
        var registration = MachineRegistrationTestData.Create();
        registration.ChangeGoLive(
            MachineRegistrationTestData.StoredDay,
            MachineRegistrationTestData.StoredDayStart,
            MachineRegistrationTestData.StoredTime);

        var result = registration.ChangeGoLive(day, MachineRegistrationTestData.OtherDayStart, changedAt);

        result.Should().Be(isChanged, caseName);
        registration.ExpectedGoLiveOn.Should().Be(isChanged ? day : MachineRegistrationTestData.StoredDay);
        registration.GoLiveStartsAt.Should()
            .Be(
                isChanged && day != MachineRegistrationTestData.StoredDay
                    ? MachineRegistrationTestData.OtherDayStart
                    : MachineRegistrationTestData.StoredDayStart);
        registration.GoLiveChangedAt.Should().Be(isChanged ? changedAt : MachineRegistrationTestData.StoredTime);
    }
}
