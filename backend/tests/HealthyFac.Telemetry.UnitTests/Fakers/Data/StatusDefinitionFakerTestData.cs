using HealthyFac.Telemetry.Domain.MachineRegistrations;

namespace HealthyFac.Telemetry.UnitTests.Fakers.Data;

public static class StatusDefinitionFakerTestData
{
    public const int ItemCount = 50;
    public const int Seed = 20261123;
    public const string GatewayId = "fakergateway0001";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    public static readonly DateTime GoLiveStartsAt =
        new(year: 2026, month: 11, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);

    public static MachineRegistration CreateRegistration() =>
        new(MachineId, GatewayId, CompanyId, DateOnly.FromDateTime(GoLiveStartsAt), GoLiveStartsAt);
}
