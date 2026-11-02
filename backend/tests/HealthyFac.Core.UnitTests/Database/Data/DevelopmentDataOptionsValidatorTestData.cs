using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(Guid.Empty), nameof(TestTenantOptions.CompanyId) },
    };

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static TestTenantOptions CreateTestTenant(Guid? companyId = null) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse("0190f3a0-0000-7000-8000-000000000001"),
            CompanyName = "Test Company",
            SupervisorIdentityId = Guid.Parse("0190f3a0-0000-7000-8000-000000000002"),
            SupervisorEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003"),
            SupervisorEmail = "supervisor@healthyfac.local",
            SupervisorFirstName = "Test",
            SupervisorLastName = "Supervisor",
        };
}
