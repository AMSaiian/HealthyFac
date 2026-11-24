namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// Names the database sessions of this process. The reset refuses to run while a session of another process uses a
/// database, and it skips the sessions with this name: they are the idle connections in the pools of this process.
/// </summary>
public static class SeederProcess
{
    /// <summary>The configuration section that holds the connection strings of a service.</summary>
    public const string ConnectionStringsSection = "ConnectionStrings";

    /// <summary>The <c>application_name</c> of every database connection of this process.</summary>
    public static readonly string ApplicationName = $"healthyfac-seeder-{Guid.NewGuid():N}";
}
