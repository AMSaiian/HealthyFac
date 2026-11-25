namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Defines the names that tie the Core gRPC clients to the identity setup.
/// </summary>
public static class CoreClientNames
{
    /// <summary>
    /// The name of the gRPC client that reads work orders from Core with the service token and actor headers.
    /// </summary>
    public const string Worker = "core-worker";

    /// <summary>The downstream scope of every Core call. The token handler adds the service name.</summary>
    public const string DownstreamScope = "work-orders";

    /// <summary>The names of the gRPC clients that call Core with the exchanged token of the user.</summary>
    public static class User
    {
        public const string Machines = "core-user-machines";
        public const string WorkOrders = "core-user-work-orders";
        public const string Employees = "core-user-employees";
        public const string Schedules = "core-user-schedules";
    }

    /// <summary>The identity of the exchanged user token.</summary>
    public static class UserToken
    {
        /// <summary>The client ID of Core. It is the audience of the exchanged user token.</summary>
        public const string Audience = "healthyfac-core";

        /// <summary>The Keycloak client scope that adds the Core audience and the chat read roles.</summary>
        public const string Scope = "insights-chat-core";
    }
}
