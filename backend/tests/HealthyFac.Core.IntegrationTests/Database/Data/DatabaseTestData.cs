using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class DatabaseTestData
{
    public const string SettingsFileName = "appsettings.Test.json";
    public const string ChangedRoleName = "Renamed by hand";
    public const string ConnectionStringKey = "ConnectionStrings:Database";
    public const string EnvironmentName = "Test";
    public const string ApplicationName = "HealthyFac.Core.IntegrationTests";

    public const int SeededCompanyCount = 2;
    public const int SeededDepartmentCount = 6;
    public const int SeededMachineCount = 24;
    public const int SeededEmployeeCount = 24;
    public const int SeededInviteCount = 28;

    public static readonly SeedingOptions ForcedRecreationOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { IsRecreationForced = true },
    };

    public static readonly Guid TestTenantCompanyId = Guid.Parse("0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid TestSupervisorIdentityId = Guid.Parse("0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid TestSupervisorEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003");
    public const string TestSupervisorEmail = "supervisor@healthyfac.local";
    public const int TestTenantDepartmentCount = 2;

    public static readonly TestTenantOptions TestTenant = new()
    {
        CompanyId = TestTenantCompanyId,
        CompanyName = "Test Company",
        SupervisorIdentityId = TestSupervisorIdentityId,
        SupervisorEmployeeId = TestSupervisorEmployeeId,
        SupervisorEmail = TestSupervisorEmail,
        SupervisorFirstName = "Test",
        SupervisorLastName = "Supervisor",
    };

    public static readonly SeedingOptions ForcedRecreationWithTestTenantOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { IsRecreationForced = true, TestTenant = TestTenant },
    };

    public static readonly SeedingOptions TestTenantOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { TestTenant = TestTenant },
    };

    public static readonly RoleCode[] RoleCodes = [RoleCode.Worker, RoleCode.Supervisor];
}
