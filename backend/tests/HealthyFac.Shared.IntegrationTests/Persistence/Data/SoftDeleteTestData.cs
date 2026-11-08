using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public static class SoftDeleteTestData
{
    public const string EntityName = "machine";

    public static readonly DateTimeOffset Now = new(
        year: 2026, month: 11, day: 4, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly Guid ActorId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");

    public static readonly ActorContext Actor = new()
    {
        Kind = ActorKind.User,
        UserId = ActorId,
    };
}
