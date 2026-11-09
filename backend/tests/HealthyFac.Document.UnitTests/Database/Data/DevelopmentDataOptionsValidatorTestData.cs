using HealthyFac.Document.Infrastructure.Database.Seeding;

namespace HealthyFac.Document.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(Guid.Empty), nameof(TestTenantOptions.CompanyId) },
    };

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static TestTenantOptions CreateTestTenant(Guid? companyId = null) =>
        new() { CompanyId = companyId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001") };
}
