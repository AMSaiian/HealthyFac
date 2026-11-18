namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;

public static class DefinitionDraftAuthorizationTestData
{
    public const string ReadPermission = "definition-drafts:read";
    public const string WritePermission = "definition-drafts:write";

    private const string AnyId = "not-checked-before-authorization";
    private const string EmptyBody = "{}";
    private const string SearchBody = "{\"limit\":25,\"filter\":{}}";

    /// <summary>
    /// The case name, the method, the path, a body, and the permission that the route needs. Authorization runs
    /// before the handler, so the IDs and the bodies need no valid values.
    /// </summary>
    public static TheoryData<string, string, string, string?, string> Routes => new()
    {
        { "SearchMachine", "POST", $"v1/machines/{AnyId}/definition-drafts/search", SearchBody, ReadPermission },
        {
            "SearchAttempt",
            "POST",
            $"v1/ingest-attempts/{AnyId}/definition-drafts/search",
            SearchBody,
            ReadPermission
        },
        { "GetAttempt", "GET", $"v1/ingest-attempts/{AnyId}/definition-drafts", null, ReadPermission },
        { "PendingCounts", "GET", "v1/definition-drafts/pending-counts", null, ReadPermission },
        { "Dismiss", "POST", $"v1/definition-drafts/{AnyId}/dismiss", EmptyBody, WritePermission },
        {
            "DismissMachine",
            "POST",
            $"v1/machines/{AnyId}/definition-drafts/dismiss",
            EmptyBody,
            WritePermission
        },
        {
            "DismissAttempt",
            "POST",
            $"v1/ingest-attempts/{AnyId}/definition-drafts/dismiss",
            EmptyBody,
            WritePermission
        },
    };
}
