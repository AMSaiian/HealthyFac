using HealthyFac.Telemetry.IntegrationTests.Intake.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

public sealed class MqttMultipleConsumersTests(MqttTwoConsumerIntakeFixture fixture)
    : IClassFixture<MqttTwoConsumerIntakeFixture>
{
    private readonly MqttTwoConsumerIntakeFixture _fixture = fixture;

    [Fact]
    public async Task Intake_WithTwoConsumers_ProcessesTwoMessagesAtTheSameTime()
    {
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var firstPayload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        var secondPayload = ReceiveMachineMessageTestData.Bytes(
            ReceiveMachineMessageTestData.CreateMessage(ReceiveMachineMessageTestData.SampledAt.AddSeconds(value: 1)));
        _fixture.OverlapProbe.Arm();

        await _fixture.PublishAllAsync(topic, [firstPayload, secondPayload]);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 2);

        messages.Should().HaveCount(expected: 2);
        _fixture.OverlapProbe.HasOverlapped.Should().BeTrue();
    }
}
