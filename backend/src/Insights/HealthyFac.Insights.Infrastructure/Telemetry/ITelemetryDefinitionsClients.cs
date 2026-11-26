using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>Gives the gRPC client of the Telemetry definitions that fits the current call.</summary>
public interface ITelemetryDefinitionsClients
{
    /// <summary>Creates the client that fits the token of the current call.</summary>
    /// <returns>The Telemetry definitions client.</returns>
    TelemetryDefinitions.TelemetryDefinitionsClient Create();
}
