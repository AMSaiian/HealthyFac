namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Starts the same services as <see cref="MqttIntakeFixture"/> with two MQTT consumers.
/// </summary>
public sealed class MqttTwoConsumerIntakeFixture : MqttIntakeFixture
{
    protected override int ConsumerCount => 2;
}
