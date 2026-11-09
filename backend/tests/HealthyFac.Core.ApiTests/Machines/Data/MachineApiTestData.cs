namespace HealthyFac.Core.ApiTests.Machines.Data;

public static class MachineApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string SearchPath = "v1/machines/search";
    public const string ReadPermission = "machines:read";
    public const string WritePermission = "machines:write";
    public const string DeletePermission = "machines:delete";
    public const string InvalidEncryptedId = "not-an-encrypted-id";
    public const string MachineName = "Lathe 01";

    public const string FileImageDescription =
        "<p><img data-file-id=\"v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\" alt=\"pump\"></p>";

    public const string SrcImageDescription = "<p><img src=\"https://example.com/a.png\"></p>";
    public const string SerialNumber = "API-001";
    public const string NewSerialNumber = "API-002";
    public const string StackTraceMarker = "   at ";

    public const string SearchWithStateFilterBody =
        "{\"offset\":0,\"limit\":25,\"sort\":{\"column\":\"name\",\"direction\":\"desc\"},"
        + "\"filter\":{\"states\":{\"values\":[\"operational\"]}}}";

    public const string SearchSortedByStateBody =
        "{\"offset\":0,\"limit\":25,\"sort\":{\"column\":\"state\",\"direction\":\"asc\"},\"filter\":{}}";

    public const string SearchOffsetDateBoundBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"createdAt\":{\"from\":\"2026-03-01T00:00:00+02:00\"}}}";

    public const string SearchUnspecifiedDateBoundBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"updatedAt\":{\"to\":\"2026-03-01T00:00:00\"}}}";

    public const string SearchNullTextValueBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"name\":{\"value\":null}}}";

    public const string SearchNullStatesBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"states\":{\"values\":null}}}";

    public static TheoryData<string> BadFilterBodies => new()
    {
        SearchOffsetDateBoundBody,
        SearchUnspecifiedDateBoundBody,
        SearchNullTextValueBody,
        SearchNullStatesBody,
    };

    public const string SearchEmptyBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
}
