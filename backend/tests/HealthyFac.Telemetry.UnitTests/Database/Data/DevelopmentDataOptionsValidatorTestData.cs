using HealthyFac.Telemetry.Infrastructure.Database.Seeding;

namespace HealthyFac.Telemetry.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static readonly Guid FirstDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");
    public static readonly Guid SecondDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b1");

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

    private static TestTenantOptions CreateTestTenant(
        Guid? companyId = null,
        IReadOnlyList<Guid>? departmentIds = null
    ) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001"),
            DepartmentIds = departmentIds ?? [FirstDepartmentId, SecondDepartmentId],
        };
}
