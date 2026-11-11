namespace HealthyFac.Insights.Infrastructure.Database;

/// <summary>
/// Defines connection, retry, health check, and error constants for the insights database.
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Lists the PostgreSQL extensions that the insights service requires: pgvector for the embeddings.
    /// </summary>
    public static readonly string[] RequiredPostgresExtensions = ["vector"];

    public const string ConnectionStringName = "Database";
    public const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Database";
    public const string HealthCheckName = "insights-database";

    public static class Retry
    {
        public const int MaxCount = 5;
        public const int MaxDelaySeconds = 10;
    }

    public static class Messages
    {
        public const string MigrationApplying = "Applying database migrations.";

        public const string MigrationSkipped =
            "Skipped automatic database migration in the {EnvironmentName} environment.";
    }

    public static class Errors
    {
        public const string Migration = "An error occurred while applying database migrations.";

        public const string MissingConnectionString =
            "Connection string 'Database' is not configured. Set ConnectionStrings__Database.";
    }
}
