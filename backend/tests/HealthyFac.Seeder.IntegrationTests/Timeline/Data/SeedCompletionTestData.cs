namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public static class SeedCompletionTestData
{
    public const string CompleteText = "The demo seed is complete.";
    public const string IncompleteText = "The demo seed is incomplete";
    public const string ResetHint = "docker compose run --rm seeder --reset";
    public const string StoppedText = "The seed job was stopped.";
    public const string FirstIngestItem = "ingest/set-1.json";
    public const string UserCountPath = "admin/realms/{0}/users/count";

    public const string ListTablesSql =
        "select table_name from information_schema.tables where table_schema = 'public' and table_type = 'BASE TABLE'";

    public const string HasUpdatedAtSql =
        "select 1 from information_schema.columns where table_schema = 'public' "
        + "and table_name = @table and column_name = 'updated_at'";

    public static readonly Guid CompanyId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c001");
}
