namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Defines the names that tie the Core HTTP client to the identity setup.
/// </summary>
public static class CoreClientNames
{
    /// <summary>The name of the HTTP client that calls Core with the service token and actor headers.</summary>
    public const string Client = "core";

    /// <summary>The downstream scope of every Core call. The token handler adds the service name.</summary>
    public const string DownstreamScope = "work-orders";
}
