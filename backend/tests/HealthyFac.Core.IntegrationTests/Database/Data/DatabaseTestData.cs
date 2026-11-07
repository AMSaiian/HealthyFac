using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
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
    public const int SeededStateHistoryCount = 42;
    public const int MachineNameNumberLength = 2;
    public const int MachinesPerDepartment = 4;
    public const int SeededEmployeeCount = 24;
    public const int SeededInviteCount = 28;

    public static readonly SeedingOptions ForcedRecreationOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { IsRecreationForced = true },
    };

    public static readonly Guid TestTenantCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid TestSupervisorIdentityId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid TestSupervisorEmployeeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000003");
    public static readonly Guid FirstTestDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");
    public static readonly Guid SecondTestDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b1");
    public const string TestSupervisorEmail = "supervisor@healthyfac.local";
    public const int TestTenantDepartmentCount = 2;

    public static readonly TestTenantOptions TestTenant = new()
    {
        CompanyId = TestTenantCompanyId,
        CompanyName = "Test Company",
        DepartmentIds = [FirstTestDepartmentId, SecondTestDepartmentId],
        SupervisorIdentityId = TestSupervisorIdentityId,
        SupervisorEmployeeId = TestSupervisorEmployeeId,
        SupervisorEmail = TestSupervisorEmail,
        SupervisorFirstName = "Test",
        SupervisorLastName = "Supervisor",
    };

    public static readonly Guid FirstSeedMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid SecondSeedMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000102");
    public const string FirstSeedGatewayId = "seedmachineone22";
    public const string SecondSeedGatewayId = "seedmachinetwo22";

    public static readonly IReadOnlyList<SeedMachineOptions> SeedMachines =
    [
        new() { MachineId = FirstSeedMachineId, GatewayId = FirstSeedGatewayId, DepartmentIndex = 0 },
        new() { MachineId = SecondSeedMachineId, GatewayId = SecondSeedGatewayId, DepartmentIndex = 1 },
    ];

    public static readonly SeedingOptions ForcedRecreationWithSeedMachinesOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions
        {
            IsRecreationForced = true,
            TestTenant = TestTenant,
            SeedMachines = SeedMachines,
        },
    };

    public static readonly SeedingOptions ForcedRecreationWithTestTenantOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { IsRecreationForced = true, TestTenant = TestTenant },
    };

    public static readonly SeedingOptions TestTenantOptions = new()
    {
        DevelopmentData = new DevelopmentDataOptions { TestTenant = TestTenant },
    };

    /// <summary>The state timeline of a seeded machine by its index modulo 3.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<MachineState>> ExpectedHistoriesByCycleIndex =
    [
        [MachineState.Operational],
        [MachineState.Operational, MachineState.InMaintenance],
        [MachineState.Operational, MachineState.InMaintenance, MachineState.OutOfService],
    ];

    public static readonly RoleCode[] RoleCodes = [RoleCode.Worker, RoleCode.Supervisor];
}
