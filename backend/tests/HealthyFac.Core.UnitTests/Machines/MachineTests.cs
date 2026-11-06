using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.UnitTests.Machines.Data;

namespace HealthyFac.Core.UnitTests.Machines;

public sealed class MachineTests
{
    [Fact]
    public void Constructor_Always_StartsOperationalAtGivenTime()
    {
        var machine = MachineTestData.CreateMachine();

        machine.StateHistory.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { State = MachineState.Operational, StartedAt = MachineTestData.StartedAt, MachineId = machine.Id }
            );
    }

    [Fact]
    public void ChangeState_WithLaterStartAndOtherState_AddsRowAsLatest()
    {
        var machine = MachineTestData.CreateMachine();

        machine.ChangeState(MachineState.InMaintenance, MachineTestData.LaterStartedAt);

        machine.StateHistory.Should().HaveCount(expected: 2);
        machine.StateHistory.MaxBy(row => row.StartedAt)
            .Should()
            .BeEquivalentTo(
                new
                {
                    State = MachineState.InMaintenance,
                    StartedAt = MachineTestData.LaterStartedAt,
                    MachineId = machine.Id,
                }
            );
    }

    [Theory]
    [MemberData(nameof(MachineTestData.NotLaterStartedAt), MemberType = typeof(MachineTestData))]
    public void ChangeState_WithStartNotAfterLatest_ThrowsAndAddsNoRow(DateTime startedAt)
    {
        var machine = MachineTestData.CreateMachine();

        var act = () => machine.ChangeState(MachineState.InMaintenance, startedAt);

        act.Should().Throw<InvalidOperationException>();
        machine.StateHistory.Should().ContainSingle();
    }

    [Fact]
    public void ChangeState_WithCurrentState_ThrowsAndAddsNoRow()
    {
        var machine = MachineTestData.CreateMachine();

        var act = () => machine.ChangeState(MachineState.Operational, MachineTestData.LaterStartedAt);

        act.Should().Throw<InvalidOperationException>();
        machine.StateHistory.Should().ContainSingle();
    }
}
