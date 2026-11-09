namespace HealthyFac.Document.Infrastructure.Database;

/// <summary>
/// Defines connection, retry, health check, and error constants for the document database.
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Lists the PostgreSQL extensions that the document service requires. The document schema needs no extension
    /// yet. Add an entry here when the document service starts to depend on one.
    /// </summary>
    public static readonly string[] RequiredPostgresExtensions = [];

    public const string ConnectionStringName = "Database";
    public const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Database";
    public const string HealthCheckName = "document-database";

    public static class Retry
    {
        public const int MaxCount = 5;
        public const int MaxDelaySeconds = 10;
    }

    public static class Messages
    {
        public const string ContainerNotCreated =
            "The file container could not be created. The first upload fails until the storage answers.";

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
