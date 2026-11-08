using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ActorTestData
{
    public static readonly Guid UserId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c1");
    public static readonly Guid EmployeeId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000e1");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c2");
    public static readonly Guid OtherUserId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a2");

    public static ActorContext CompanyActor() =>
        new()
        {
            Kind = ActorKind.User,
            UserId = UserId,
            CompanyId = CompanyId,
        };

    public static ActorContext UserActor(Guid userId) =>
        new()
        {
            Kind = ActorKind.User,
            UserId = userId,
        };
}
