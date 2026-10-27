namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public static class NpgsqlTracingTestData
{
    public const string SourceName = "Npgsql";
    public const string ParentName = "test-parent";
    public const string QueryTextTag = "db.query.text";

    public static string CreateMarkerSql(string marker) => $"SELECT '{marker}'";

    public static string CreateMarker() => $"marker-{Guid.CreateVersion7():N}";
}
