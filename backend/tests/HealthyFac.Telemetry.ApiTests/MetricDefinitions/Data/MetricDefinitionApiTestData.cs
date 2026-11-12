namespace HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;

public static class MetricDefinitionApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string DefinitionsSegment = "metric-definitions";
    public const string SchemaSegment = "metric-schema";
    public const string ReadPermission = "metric-definitions:read";
    public const string WritePermission = "metric-definitions:write";
    public const string InvalidEncryptedId = "not-an-encrypted-id";
    public const string Key = "spindle.temperature";

    public static TheoryData<string, string> ReadRoutesWithoutReadPermission => new()
    {
        { "ListDefinitions", MachinesPath + "/{machineId}/" + DefinitionsSegment },
        { "GetSchema", MachinesPath + "/{machineId}/" + SchemaSegment },
    };

    public const string CreateBody =
        "{\"expectedRevision\":0,\"created\":[{\"key\":\"spindle.temperature\",\"name\":\"Spindle temperature\","
        + "\"valueKind\":\"numeric\",\"unit\":{\"quantityKind\":\"Temperature\",\"name\":\"DegreeCelsius\"},"
        + "\"min\":-20,\"max\":200}]}";

    public const string StaleBody = "{\"expectedRevision\":7}";

    public const string InvalidKeyBody =
        "{\"expectedRevision\":0,\"created\":[{\"key\":\"Bad Key\",\"name\":\"Name\",\"valueKind\":\"boolean\"}]}";

    public const string InvalidDeletedIdBody = "{\"expectedRevision\":0,\"deleted\":[\"not-an-encrypted-id\"]}";
}
