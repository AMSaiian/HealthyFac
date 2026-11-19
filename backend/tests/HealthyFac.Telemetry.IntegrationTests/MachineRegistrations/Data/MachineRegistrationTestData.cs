using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

public static class MachineRegistrationTestData
{
    public const int RaceParticipantCount = 2;

    public const string MachineName = "Test Machine";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a02");

    public static MachineCreated CreateMessage() =>
        new()
        {
            MachineId = Guid.CreateVersion7(),
            GatewayId = GatewayIds.Create(),
            CompanyId = CompanyId,
            Name = MachineName,
            ExpectedGoLiveOn = InitialGoLiveDay,
            GoLiveStartsAt = InitialGoLiveStart,
        };

    public static readonly DateOnly InitialGoLiveDay = new(year: 2020, month: 1, day: 1);

    public static readonly DateTime InitialGoLiveStart = new(
        year: 2019, month: 12, day: 31, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateOnly GoLiveDay = new(year: 2026, month: 12, day: 1);

    public static readonly DateTime GoLiveStart = new(
        year: 2026, month: 11, day: 30, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateOnly OtherGoLiveDay = new(year: 2026, month: 12, day: 2);

    public static readonly DateTime OtherGoLiveStart = new(
        year: 2026, month: 12, day: 1, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime StateStart = new(
        year: 2026, month: 11, day: 10, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static MachineStateChanged CreateStateMessage(Guid machineId, MachineStateValue state, DateTime startedAt) =>
        new() { MachineId = machineId, CompanyId = CompanyId, State = state, StartedAt = startedAt };

    public static MachineGoLiveDateChanged CreateGoLiveMessage(
        Guid machineId,
        DateOnly day,
        DateTime startsAt,
        DateTime changedAt
    ) =>
        new()
        {
            MachineId = machineId,
            CompanyId = CompanyId,
            ExpectedGoLiveOn = day,
            GoLiveStartsAt = startsAt,
            ChangedAt = changedAt,
        };

    public static MachineDeleted CreateDeletedMessage(Guid machineId) =>
        new() { MachineId = machineId, CompanyId = CompanyId };
}
