using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Publishes machine messages through the broker of the intake fixture and waits for what the intake stored.
/// </summary>
public static class MqttIntakeFixtureExtensions
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(seconds: 30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(milliseconds: 100);

    extension(MqttIntakeFixture fixture)
    {
        public Task PublishAllAsync(string topic, IEnumerable<byte[]> payloads) =>
            Task.WhenAll(payloads.Select(payload => fixture.PublishAsync(topic, payload)));

        public async Task<IReadOnlyList<ReceivedMessage>> WaitForMessagesAsync(Guid machineId, int expectedCount)
        {
            IReadOnlyList<ReceivedMessage> messages = [];
            var deadline = DateTime.UtcNow + WaitLimit;

            while (messages.Count < expectedCount && DateTime.UtcNow < deadline)
            {
                messages = await fixture.Database.LoadReceivedMessagesAsync(machineId);

                if (messages.Count < expectedCount)
                {
                    await Task.Delay(PollInterval);
                }
            }

            return messages;
        }

        public async Task WaitUntilConnectedAsync()
        {
            var deadline = DateTime.UtcNow + WaitLimit;

            while (!fixture.State.IsConnected && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval);
            }
        }

        public async Task<IReadOnlyList<ReceivedMessage>> WaitForTopicMessagesAsync(string topic, int expectedCount)
        {
            IReadOnlyList<ReceivedMessage> messages = [];
            var deadline = DateTime.UtcNow + WaitLimit;

            while (messages.Count < expectedCount && DateTime.UtcNow < deadline)
            {
                messages = await fixture.Database.LoadReceivedMessagesByTopicAsync(topic);

                if (messages.Count < expectedCount)
                {
                    await Task.Delay(PollInterval);
                }
            }

            return messages;
        }
    }
}
