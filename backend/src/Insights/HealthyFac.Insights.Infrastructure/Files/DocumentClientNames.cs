namespace HealthyFac.Insights.Infrastructure.Files;

/// <summary>
/// Defines the names that tie the Document gRPC clients to the identity setup.
/// </summary>
public static class DocumentClientNames
{
    /// <summary>The name of the client that calls with the exchanged token of the user.</summary>
    public const string User = "document-user";

    /// <summary>The name of the client that calls with the service token and the actor headers.</summary>
    public const string Worker = "document-worker";

    /// <summary>The client ID of Document. It is the audience of the exchanged user token.</summary>
    public const string Audience = "healthyfac-document";

    /// <summary>The Keycloak client scope that adds the Document audience and the file read role.</summary>
    public const string UserTokenScope = "insights-files";

    /// <summary>The downstream scope of the worker call. The token handler adds the service name.</summary>
    public const string WorkerDownstreamScope = "files";
}
