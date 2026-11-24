namespace HealthyFac.Seeder.Reset;

/// <summary>Defines the messages of the reset.</summary>
public static class ResetErrors
{
    public static string DatabaseInUse(string database) =>
        $"The database {database} has a session of another process. Stop the services first: "
        + "docker compose stop core telemetry document insights auth";
}
