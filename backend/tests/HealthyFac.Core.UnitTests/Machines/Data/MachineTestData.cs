using Bogus;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.UnitTests.Machines.Data;

public static class MachineTestData
{
    private const int RandomSeed = 20261106;

    private static readonly Faker Faker = new() { Random = new Randomizer(RandomSeed) };

    public static readonly Guid CompanyId = Faker.Random.Guid();
    public static readonly Guid DepartmentId = Faker.Random.Guid();
    public static readonly string Name = Faker.Commerce.ProductName();
    public static readonly string SerialNumber = Faker.Random.AlphaNumeric(length: 12);

    public static readonly DateTime StartedAt = new(
        year: 2026, month: 11, day: 6, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime LaterStartedAt = StartedAt.AddHours(value: 1);

    public static TheoryData<DateTime> NotLaterStartedAt =>
    [
        StartedAt,
        StartedAt.AddTicks(value: -1),
        StartedAt.AddDays(value: -1),
    ];

    public static Machine CreateMachine() =>
        new(CompanyId, DepartmentId, GatewayIds.Create(), Name, SerialNumber, description: null, StartedAt);
}
