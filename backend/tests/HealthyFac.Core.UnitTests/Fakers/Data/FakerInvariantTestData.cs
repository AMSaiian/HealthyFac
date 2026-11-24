using HealthyFac.Core.Domain.Roles;

namespace HealthyFac.Core.UnitTests.Fakers.Data;

public static class FakerInvariantTestData
{
    public const int ItemCount = 50;
    public const int Seed = 20261123;
    public const string CompanyZone = "Europe/Kyiv";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid RoleId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002");
    public static readonly RoleCode InviteRole = RoleCode.Worker;

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 23, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(value: 3);
}
