using HealthyFac.Shared.Infrastructure.Messaging.Sending;

namespace HealthyFac.Seeder.UnitTests.Relay.Data;

public static class RelayTestData
{
    public const string Event = "orders.json#order-1";

    private static readonly DateTime Start = new(
        year: 2027, month: 3, day: 24, hour: 7, minute: 0, second: 0, DateTimeKind.Utc);

    public static OutboxMessage Row(string type, int minute) =>
        new(
            type,
            payload: "{}",
            Start.AddMinutes(minute),
            traceParent: null,
            Guid.Parse(input: "019b4c00-0000-7000-8000-00000000a001"),
            companyId: null);
}
