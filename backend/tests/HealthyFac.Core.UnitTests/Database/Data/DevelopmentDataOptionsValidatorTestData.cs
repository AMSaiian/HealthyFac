using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(Guid.Empty), nameof(TestTenantOptions.CompanyId) },
        { CreateTestTenant(departmentIds: [FirstDepartmentId]), nameof(TestTenantOptions.DepartmentIds) },
        { CreateTestTenant(departmentIds: [FirstDepartmentId, Guid.Empty]), nameof(TestTenantOptions.DepartmentIds) },
        {
            CreateTestTenant(departmentIds: [FirstDepartmentId, FirstDepartmentId]),
            nameof(TestTenantOptions.DepartmentIds)
        },
    };

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static readonly Guid FirstDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");
    private static readonly Guid SecondDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b1");

    private static TestTenantOptions CreateTestTenant(
        Guid? companyId = null,
        IReadOnlyList<Guid>? departmentIds = null
    ) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001"),
            CompanyName = "Test Company",
            DepartmentIds = departmentIds ?? [FirstDepartmentId, SecondDepartmentId],
            SupervisorIdentityId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002"),
            SupervisorEmployeeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000003"),
            SupervisorEmail = "supervisor@healthyfac.local",
            SupervisorFirstName = "Test",
            SupervisorLastName = "Supervisor",
        };
}
