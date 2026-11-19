namespace HealthyFac.Insights.Infrastructure.Telemetry;

public static class TelemetryClientMessages
{
    public const string BaseAddressInvalid = "TelemetryClient:BaseAddress must be an absolute http or https URI.";
    public const string GrpcAddressInvalid = "TelemetryClient:GrpcAddress must be an absolute http or https URI.";
}
