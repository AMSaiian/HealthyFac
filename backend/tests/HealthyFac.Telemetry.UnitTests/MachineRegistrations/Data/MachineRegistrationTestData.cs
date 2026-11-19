using HealthyFac.Telemetry.Domain.MachineRegistrations;

namespace HealthyFac.Telemetry.UnitTests.MachineRegistrations.Data;

public static class MachineRegistrationTestData
{
    public static readonly DateTime StoredTime = new(
        year: 2026, month: 11, day: 10, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateOnly StoredDay = new(year: 2026, month: 12, day: 1);

    public static readonly DateOnly OtherDay = new(year: 2026, month: 12, day: 2);

    public static readonly DateTime StoredDayStart = new(
        year: 2026, month: 11, day: 30, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime OtherDayStart = new(
        year: 2026, month: 12, day: 1, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static TheoryData<string, DateTime, bool> StateStarts => new()
    {
        { "older start", StoredTime.AddMinutes(value: -1), false },
        { "equal start", StoredTime, false },
        { "newer start", StoredTime.AddMinutes(value: 1), true },
    };

    public static TheoryData<string, DateTime, DateOnly, bool> GoLiveChanges => new()
    {
        { "older change", StoredTime.AddMinutes(value: -1), OtherDay, false },
        { "equal change", StoredTime, OtherDay, false },
        { "newer change with the stored day", StoredTime.AddMinutes(value: 1), StoredDay, true },
        { "newer change with another day", StoredTime.AddMinutes(value: 1), OtherDay, true },
    };

    public static MachineRegistration Create() =>
        new(
            Guid.CreateVersion7(),
            gatewayId: "abcdefghijklmnop",
            Guid.CreateVersion7(),
            StoredDay,
            StoredDayStart);
}
