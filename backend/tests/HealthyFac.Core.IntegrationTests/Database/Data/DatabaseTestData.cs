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

    public static readonly RoleCode[] RoleCodes = [RoleCode.Worker, RoleCode.Supervisor];
}
