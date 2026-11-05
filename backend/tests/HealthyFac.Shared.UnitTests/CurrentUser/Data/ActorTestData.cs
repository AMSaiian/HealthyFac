using System.Collections.Frozen;
using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ActorTestData
{
    public static readonly Guid UserId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c1");
    public static readonly Guid EmployeeId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000e1");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c2");
    public static readonly Guid FirstDepartmentId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000d1");
    public static readonly Guid SecondDepartmentId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000d2");
    public static readonly Guid ForeignDepartmentId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000d3");
    public static readonly Guid OtherUserId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a2");

    public static ActorContext DepartmentActor(params Guid[] departmentIds) =>
        new()
        {
            Kind = ActorKind.User,
            UserId = UserId,
            CompanyId = CompanyId,
            DepartmentIds = departmentIds.ToFrozenSet(),
        };

    public static ActorContext UserActor(Guid userId) =>
        new()
        {
            Kind = ActorKind.User,
            UserId = userId,
            DepartmentIds = FrozenSet<Guid>.Empty,
        };
}
