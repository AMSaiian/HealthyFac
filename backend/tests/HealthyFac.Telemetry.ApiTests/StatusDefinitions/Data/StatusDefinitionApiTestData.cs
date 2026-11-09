namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

public static class StatusDefinitionApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string StatusDefinitionsSegment = "status-definitions";
    public const string SearchSegment = "search";
    public const string ReadPermission = "status-definitions:read";
    public const string WritePermission = "status-definitions:write";
    public const string DeletePermission = "status-definitions:delete";
    public const string InvalidEncryptedId = "not-an-encrypted-id";
    public const string Severity = "warning";
    public const string CreateWithoutSeverityBody = "{\"code\":\"E-100\"}";
    public const string Code = "E-100";

    public const string FileImageDescription =
        "<p><img data-file-id=\"v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\" alt=\"pump\"></p>";

    public const string SrcImageDescription = "<p><img src=\"https://example.com/a.png\"></p>";
    public const string UpdatedCode = "E-200";
    public const string SearchEmptyBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
}
