using System.Text;
using HealthyFac.Testing.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Publishes a machine message with MQTT 5 user properties, which the plain publish of the fixture has no room for.
/// </summary>
public static class IntakeObservabilityFixtureExtensions
{
    extension(MqttIntakeFixture fixture)
    {
        public async Task PublishWithPropertyAsync(
            string topic,
            byte[] payload,
            string propertyName,
            string propertyValue
        )
        {
            using var sender = new MqttClientFactory().CreateMqttClient();
            await sender.ConnectAsync(
                new MqttClientOptionsBuilder()
                    .WithTcpServer(fixture.Broker.Host, fixture.Broker.Port)
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                    .WithCredentials(MosquittoTestBroker.SenderUser, fixture.Broker.SenderPassword)
                    .Build());

            await sender.PublishAsync(
                new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithUserProperty(propertyName, Encoding.UTF8.GetBytes(propertyValue))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());
            await sender.DisconnectAsync();
        }
    }
}
