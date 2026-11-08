using HealthyFac.Telemetry.IntegrationTests.Intake.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

[Collection(TelemetryMqttCollection.Name)]
public sealed class MqttMultipleConsumersTests(MqttIntakeFixture fixture)
    : IAsyncLifetime
{
    private const int ConsumerCount = 2;

    private readonly MqttIntakeFixture _fixture = fixture;

    private StartedIntake _intake = null!;

    public async ValueTask InitializeAsync()
    {
        _intake = _fixture.CreateIntake(NewName(), NewName(), consumerCount: ConsumerCount);
        await _intake.Service.StartAsync(CancellationToken.None);
        await MqttIntakeFixture.WaitUntilConnectedAsync(_intake, ConsumerCount);
    }

    public async ValueTask DisposeAsync()
    {
        await _intake.Service.StopAsync(CancellationToken.None);
        await _intake.DisposeAsync();
    }

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

    private static string NewName() => Guid.NewGuid().ToString(format: "N");
}
