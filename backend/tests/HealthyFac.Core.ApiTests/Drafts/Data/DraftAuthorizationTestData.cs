namespace HealthyFac.Core.ApiTests.Drafts.Data;

public static class DraftAuthorizationTestData
{
    private const string AnyId = "not-checked-before-authorization";
    private const string EmptyBody = "{}";
    private const string SearchBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
    private const string ApplyBody = "{\"name\":\"n\",\"serialNumber\":\"s\"}";
    private const string MarkBody = "{\"ids\":[]}";

    /// <summary>
    /// Every draft endpoint: the method, the path, and a body. Authorization runs before the handler, so the IDs and
    /// the bodies need no valid values.
    /// </summary>
    public static TheoryData<string, string, string?> Endpoints => new()
    {
        { "GET", $"v1/machine-drafts/{AnyId}", null },
        { "POST", $"v1/machine-drafts/{AnyId}/apply", ApplyBody },
        { "POST", $"v1/machines/{AnyId}/drafts/search", SearchBody },
        { "GET", $"v1/ingest-attempts/{AnyId}/drafts", null },
        { "POST", $"v1/drafts/{AnyId}/dismiss", EmptyBody },
        { "POST", "v1/drafts/mark-applied", MarkBody },
        { "POST", $"v1/machines/{AnyId}/drafts/dismiss", EmptyBody },
    };
}
