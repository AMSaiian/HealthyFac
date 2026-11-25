namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Defines the names that tie the Core gRPC client to the identity setup.
/// </summary>
public static class CoreClientNames
{
    /// <summary>
    /// The name of the gRPC client that reads work orders from Core with the service token and actor headers.
    /// </summary>
    public const string Worker = "core-worker";

    /// <summary>The downstream scope of every Core call. The token handler adds the service name.</summary>
    public const string DownstreamScope = "work-orders";
}
