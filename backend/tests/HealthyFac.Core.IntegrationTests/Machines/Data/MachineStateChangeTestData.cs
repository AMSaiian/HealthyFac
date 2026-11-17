using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.IntegrationTests.Machines.Data;

public static class MachineStateChangeTestData
{
    public static readonly Guid ActorId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");

    public static readonly DateTimeOffset CreatedNow = new(
        year: 2026, month: 3, day: 10, hour: 9, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly DateTimeOffset ChangedNow = new(
        year: 2026, month: 3, day: 11, hour: 9, minute: 0, second: 0, TimeSpan.Zero);

    public static TheoryData<MachineState> StatesOtherThanOperational => new()
    {
        MachineState.InMaintenance,
        MachineState.OutOfService,
    };
}
