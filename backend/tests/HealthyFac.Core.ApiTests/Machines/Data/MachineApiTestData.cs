namespace HealthyFac.Core.ApiTests.Machines.Data;

public static class MachineApiTestData
{
    public const string FutureGoLiveOn = "2099-01-01";
    public const string MachinesPath = "v1/machines";
    public const string SearchPath = "v1/machines/search";
    public const string LookupPath = "v1/machines/lookup";
    public const string NamesPath = "v1/machines/names";
    public const int NameLookupTooManyIds = 101;
    public const string LookupEmptyBody = "{}";
    public const string ReadPermission = "machines:read";
    public const string WritePermission = "machines:write";
    public const string DeletePermission = "machines:delete";
    public const string InvalidEncryptedId = "not-an-encrypted-id";
    public const string MachineName = "Lathe 01";

    public const string FileImageDescription =
        "<p><img data-file-id=\"v1_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\" alt=\"pump\"></p>";

    public const string SrcImageDescription = "<p><img src=\"https://example.com/a.png\"></p>";
    public const string SerialNumber = "API-001";
    public const string NewSerialNumber = "API-002";
    public const string GoLiveSerialNumber = "API-GOLIVE";
    public const string NoGoLiveSerialNumber = "API-NO-GOLIVE";
    public const string OperationalState = "operational";
    public const string StatePathSuffix = "/state";
    public const string ValidStateBody = "{\"state\":\"inMaintenance\"}";
    public const string UnknownStateBody = "{\"state\":\"broken\"}";
    public const string MissingStateBody = "{}";

    public const string UpdateWithoutStateBody =
        "{\"name\":\"Lathe 01\",\"serialNumber\":\"API-NO-STATE\",\"expectedGoLiveOn\":\"2099-01-01\"}";

    public const string UpdateWithUnknownStateBody =
        "{\"name\":\"Lathe 01\",\"serialNumber\":\"API-BAD-STATE\",\"expectedGoLiveOn\":\"2099-01-01\","
        + "\"state\":\"broken\"}";

    public const string StackTraceMarker = "   at ";

    public const string GoLiveJson = "\"expectedGoLiveOn\":\"2099-01-01\"";

    public const string SearchWithBackwardsGoLiveRangeBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"expectedGoLiveOn\":"
        + "{\"from\":\"2099-01-03\",\"to\":\"2099-01-02\"}}}";

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

    public static TheoryData<string> BadFilterBodies =>
    [
        SearchOffsetDateBoundBody,
        SearchUnspecifiedDateBoundBody,
        SearchNullTextValueBody,
        SearchNullStatesBody,
    ];

    public static TheoryData<string, string> BadStateBodies => new()
    {
        { "Missing", MissingStateBody },
        { "Unknown", UnknownStateBody },
    };

    public static TheoryData<string, string> BadUpdateStateBodies => new()
    {
        { "Missing", UpdateWithoutStateBody },
        { "Unknown", UpdateWithUnknownStateBody },
    };

    public const string SearchEmptyBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
}
