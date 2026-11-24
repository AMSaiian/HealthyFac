using HealthyFac.Core.Domain.Roles;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class DatabaseTestData
{
    public const string SettingsFileName = "appsettings.Test.json";
    public const string ChangedRoleName = "Renamed by hand";
    public const string ConnectionStringKey = "ConnectionStrings:Database";
    public const string EnvironmentName = "Test";
    public const string ApplicationName = "HealthyFac.Core.IntegrationTests";

    public static readonly RoleCode[] RoleCodes = [RoleCode.Worker, RoleCode.Supervisor];
}
