using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// A consumer that a stop or start test created, with its connection state.
/// </summary>
public sealed class StartedIntake
{
    /// <summary>The consumer service. The test starts and stops it.</summary>
    public required MqttIntakeService Service { get; init; }

    /// <summary>The connection state of the service.</summary>
    public required MqttIntakeState State { get; init; }
}
