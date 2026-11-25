namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public static class SeedCompletionTestData
{
    public const string CompleteText = "The demo seed is complete.";
    public const string IncompleteText = "The demo seed is incomplete";
    public const string ResetHint = "docker compose run --rm seeder --reset";
    public const string NoCompleteHistoryText = "has no complete telemetry history";
    public const string TelemetryArgument = "--telemetry";
    public const string ResetTelemetryHint = "docker compose run --rm seeder --reset --telemetry";
    public const string StoppedText = "The seed job was stopped.";
    public const string FirstIngestItem = "ingest/set-1.json";
    public const string UserCountPath = "admin/realms/{0}/users/count";

    public const string ListTablesSql =
        "select table_name from information_schema.tables where table_schema = 'public' and table_type = 'BASE TABLE'";

    public const string HasUpdatedAtSql =
        "select 1 from information_schema.columns where table_schema = 'public' "
        + "and table_name = @table and column_name = 'updated_at'";

    /// <summary>The <c>markerMessageId</c> of the committed <c>telemetry.json</c>.</summary>
    public static readonly Guid MarkerMessageId = Guid.Parse(input: "01a20ec8-2b3f-7d0f-908e-f5568348dd43");

    public static readonly Guid OtherMessageId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c0aa");

    public const string InsertReceivedMessageSql =
        "INSERT INTO received_messages (id, topic, received_at, payload, errors) "
        + "VALUES (@id, 'seed', now(), '[]'::jsonb, '[]'::jsonb)";

    public static readonly Guid CompanyId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c001");
}
