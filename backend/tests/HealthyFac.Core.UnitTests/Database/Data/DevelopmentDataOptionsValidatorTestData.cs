using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.UnitTests.Database.Data;

public static class DevelopmentDataOptionsValidatorTestData
{
    public static TheoryData<TestTenantOptions, string> InvalidTestTenants => new()
    {
        { CreateTestTenant(companyId: Guid.Empty), nameof(TestTenantOptions.CompanyId) },
        { CreateTestTenant(supervisorIdentityId: Guid.Empty), nameof(TestTenantOptions.SupervisorIdentityId) },
        { CreateTestTenant(supervisorEmployeeId: Guid.Empty), nameof(TestTenantOptions.SupervisorEmployeeId) },
        { CreateTestTenant(supervisorEmail: "not-an-email"), nameof(TestTenantOptions.SupervisorEmail) },
        { CreateTestTenant(companyName: " "), nameof(TestTenantOptions.CompanyName) },
        { CreateTestTenant(supervisorFirstName: string.Empty), nameof(TestTenantOptions.SupervisorFirstName) },
        { CreateTestTenant(supervisorLastName: " "), nameof(TestTenantOptions.SupervisorLastName) },
    };

    public static TestTenantOptions ValidTestTenant => CreateTestTenant();

    private static TestTenantOptions CreateTestTenant(
        Guid? companyId = null,
        Guid? supervisorIdentityId = null,
        Guid? supervisorEmployeeId = null,
        string supervisorEmail = "supervisor@healthyfac.local",
        string companyName = "Test Company",
        string supervisorFirstName = "Test",
        string supervisorLastName = "Supervisor"
    ) =>
        new()
        {
            CompanyId = companyId ?? Guid.Parse("0190f3a0-0000-7000-8000-000000000001"),
            CompanyName = companyName,
            SupervisorIdentityId = supervisorIdentityId ?? Guid.Parse("0190f3a0-0000-7000-8000-000000000002"),
            SupervisorEmployeeId = supervisorEmployeeId ?? Guid.Parse("0190f3a0-0000-7000-8000-000000000003"),
            SupervisorEmail = supervisorEmail,
            SupervisorFirstName = supervisorFirstName,
            SupervisorLastName = supervisorLastName,
        };
}
