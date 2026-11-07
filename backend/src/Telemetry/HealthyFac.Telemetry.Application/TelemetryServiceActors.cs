using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Telemetry.Application;

/// <summary>
/// Declares the flows of the telemetry service that act as a service actor. Shared flows (the database start-up and
/// the message processors) live in <see cref="ServiceActor"/>.
/// </summary>
public static class TelemetryServiceActors
{
    public const string IntakeFlow = "intake";

    /// <summary>The actor of the MQTT consumer, named <c>telemetry:intake</c>. It acts for no company.</summary>
    public static ActorContext Intake { get; } = KnownActors.ForServiceFlow(ServiceName.Telemetry, IntakeFlow);
}
