using HealthyFac.Document.Infrastructure.Database.Seeding;

namespace HealthyFac.Document.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(Guid.Empty), nameof(TestTenantOptions.CompanyId) },
        { CreateTestTenant(supervisorIdentityId: Guid.Empty), nameof(TestTenantOptions.SupervisorIdentityId) },
    };

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static TestTenantOptions CreateTestTenant(Guid? companyId = null, Guid? supervisorIdentityId = null) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001"),
            SupervisorIdentityId = supervisorIdentityId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002"),
        };
}
