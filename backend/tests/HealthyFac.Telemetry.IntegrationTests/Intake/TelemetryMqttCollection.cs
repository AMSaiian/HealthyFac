using HealthyFac.Telemetry.IntegrationTests.Intake.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

[CollectionDefinition(Name)]
public sealed class TelemetryMqttCollection : ICollectionFixture<MqttIntakeFixture>
{
    public const string Name = "TelemetryMqtt";
}
