using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(Guid.Empty), nameof(TestTenantOptions.CompanyId) },
    };

    public const string UnknownTimeZoneId = "Not/AZone";

    public static TestTenantOptions TestTenantWithUnknownTimeZone => CreateTestTenant(timeZoneId: UnknownTimeZoneId);

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static TestTenantOptions CreateTestTenant(
        Guid? companyId = null,
        string timeZoneId = "Europe/Kyiv"
    ) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001"),
            CompanyName = "Test Company",
            TimeZoneId = timeZoneId,
            SupervisorIdentityId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002"),
            SupervisorEmployeeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000003"),
            SupervisorEmail = "supervisor@healthyfac.local",
            SupervisorFirstName = "Test",
            SupervisorLastName = "Supervisor",
        };
}
