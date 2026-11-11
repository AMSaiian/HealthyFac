namespace HealthyFac.Insights.Web.Ingest;

/// <summary>
/// Defines route constants for the ingest endpoints.
/// </summary>
public static class IngestRoutes
{
    public const string FileIdParameter = "fileId";
    public const string IdParameter = "id";
    public const string FileBaseRoute = "/files/{" + FileIdParameter + "}/ingest-attempts";
    public const string AttemptBaseRoute = "/ingest-attempts";
    public const string Root = "/";
    public const string ById = "/{" + IdParameter + "}";
    public const string Cancel = ById + "/cancel";

    /// <summary>The path segment that starts the file routes. The location of a new attempt replaces it.</summary>
    public const string FilesSegment = "/files/";

    /// <summary>
    /// The format of the location of an attempt: the path up to the version, the route, the encrypted ID.
    /// </summary>
    public const string AttemptLocationFormat = "{0}" + AttemptBaseRoute + "/{1}";
}
