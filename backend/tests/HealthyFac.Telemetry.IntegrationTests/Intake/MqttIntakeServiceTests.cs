using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

[Collection(TelemetryMqttCollection.Name)]
public sealed class MqttIntakeServiceTests(MqttIntakeFixture fixture) : IAsyncLifetime
{
    private readonly MqttIntakeFixture _fixture = fixture;

    private StartedIntake _intake = null!;

    public async ValueTask InitializeAsync()
    {
        _intake = _fixture.CreateIntake(NewName(), NewName());
        await _intake.Service.StartAsync(CancellationToken.None);
        await MqttIntakeFixture.WaitUntilConnectedAsync(_intake);
    }

    public async ValueTask DisposeAsync()
    {
        await _intake.Service.StopAsync(CancellationToken.None);
        await _intake.DisposeAsync();
    }

    [Fact]
    public async Task Intake_WithPublishedValidMessage_StoresMessage()
    {
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        await _fixture.PublishAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 1);
        var readings = await _fixture.Database.LoadReadingsAsync(machine.MachineId);

        messages.Single().Errors.Should().BeEmpty();
        readings.Total.Should().Be(expected: 5);
    }

    [Fact]
    public async Task Intake_WithTransientFailureOnce_RedeliversAndStoresOnce()
    {
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        var savesBefore = _fixture.SaveFailure.SaveCount;
        _fixture.SaveFailure.Arm(new TimeoutException());

        await _fixture.PublishAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 1);
        await Task.Delay(TimeSpan.FromSeconds(seconds: 2), TestContext.Current.CancellationToken);
        var messagesAfter = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);

        messages.Should().ContainSingle();
        messagesAfter.Should().ContainSingle();
        (_fixture.SaveFailure.SaveCount - savesBefore).Should().Be(expected: 2);
    }

    [Fact]
    public async Task Intake_WithNonTransientFailure_StoresProcessingFailedAndAcknowledges()
    {
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        _fixture.SaveFailure.Arm(new InvalidOperationException(message: "The test made this save fail."));

        await _fixture.PublishAsync(topic, payload);

        await _fixture.WaitForTopicMessagesAsync(topic, expectedCount: 1);
        _fixture.SaveFailure.Arm(new TimeoutException());
        var nextSampledAt = ReceiveMachineMessageTestData.SampledAt.AddSeconds(value: 1);
        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage(nextSampledAt)));

        var storedForMachine = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 1);
        await Task.Delay(TimeSpan.FromSeconds(seconds: 2), TestContext.Current.CancellationToken);
        var onTopic = await _fixture.Database.LoadReceivedMessagesByTopicAsync(topic);
        var storedForMachineAfter = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);
        var failed = onTopic
            .Where(message => message.Errors.Any(error => error.Code == IntakeErrorCodes.ProcessingFailed))
            .ToList();

        failed.Should().ContainSingle();
        failed.Single().Payload.Should().BeNull();
        failed.Single().RawPayload.Should().Equal(payload);
        storedForMachine.Should().ContainSingle();
        storedForMachineAfter.Should().ContainSingle();
    }

    [Fact]
    public async Task HealthCheck_WhileConnected_IsHealthy()
    {
        var result = await CreateHealthCheck()
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_WhileNotConnected_IsDegraded()
    {
        var state = new MqttIntakeState();

        var result = await new MqttIntakeHealthCheck(
                Options.Create(new MqttIntakeOptions { IsEnabled = true }),
                state)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task HealthCheck_WithOneOfTwoConsumersConnected_IsDegraded()
    {
        var state = new MqttIntakeState();
        state.MarkConnected();

        var result = await new MqttIntakeHealthCheck(
                Options.Create(new MqttIntakeOptions { IsEnabled = true, ConsumerCount = 2 }),
                state)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task HealthCheck_WithAllTwoConsumersConnected_IsHealthy()
    {
        var state = new MqttIntakeState();
        state.MarkConnected();
        state.MarkConnected();

        var result = await new MqttIntakeHealthCheck(
                Options.Create(new MqttIntakeOptions { IsEnabled = true, ConsumerCount = 2 }),
                state)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_WhenDisabled_IsHealthy()
    {
        var result = await new MqttIntakeHealthCheck(
                Options.Create(new MqttIntakeOptions { IsEnabled = false }),
                new MqttIntakeState())
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    private static string NewName() => Guid.NewGuid().ToString(format: "N");

    private MqttIntakeHealthCheck CreateHealthCheck() =>
        new(Options.Create(new MqttIntakeOptions { IsEnabled = true }), _intake.State);
}
