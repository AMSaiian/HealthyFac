namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Defines the names that tie the Telemetry clients to the identity setup.
/// </summary>
public static class TelemetryClientNames
{
    /// <summary>The name of the gRPC client that reads the definitions and the units with the service token.</summary>
    public const string Definitions = "telemetry-definitions";

    /// <summary>The name of the gRPC client that reads the signals from Telemetry.</summary>
    public const string Signals = "telemetry-signals";

    /// <summary>The downstream scope of every Telemetry call. The token handler adds the service name.</summary>
    public const string DownstreamScope = "telemetry";

    /// <summary>The names of the gRPC clients that call Telemetry with the exchanged token of the user.</summary>
    public static class User
    {
        public const string Signals = "telemetry-user-signals";
        public const string Definitions = "telemetry-user-definitions";
    }

    /// <summary>The identity of the exchanged user token.</summary>
    public static class UserToken
    {
        /// <summary>The client ID of Telemetry. It is the audience of the exchanged user token.</summary>
        public const string Audience = "healthyfac-telemetry";

        /// <summary>The Keycloak client scope that adds the Telemetry audience and the chat read roles.</summary>
        public const string Scope = "insights-chat-telemetry";
    }
}
