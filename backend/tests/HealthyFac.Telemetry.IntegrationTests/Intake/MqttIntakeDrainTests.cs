using System.Buffers;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using MQTTnet;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

public sealed class MqttIntakeDrainTests(MqttDrainFixture fixture) : IClassFixture<MqttDrainFixture>
{
    private static readonly TimeSpan SlowSaveDelay = TimeSpan.FromSeconds(seconds: 3);
    private static readonly TimeSpan VerySlowSaveDelay = TimeSpan.FromSeconds(seconds: 20);

    private readonly MqttDrainFixture _fixture = fixture;

    [Fact]
    public async Task Restart_OfSingleInstance_StoresMessagesPublishedWhileStopped()
    {
        var group = NewName();
        var clientId = NewName();
        var first = _fixture.CreateIntake(group, clientId);
        await first.Service.StartAsync(CancellationToken.None);
        await MqttDrainFixture.WaitUntilConnectedAsync(first);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        await first.Service.StopAsync(CancellationToken.None);
        await _fixture.PublishAllAsync(topic, Payloads(offset: 0, IntakeTestData.MessageCount));
        var second = _fixture.CreateIntake(group, clientId);

        await second.Service.StartAsync(CancellationToken.None);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, IntakeTestData.MessageCount);
        await second.Service.StopAsync(CancellationToken.None);

        messages.Should().HaveCount(IntakeTestData.MessageCount);
    }

    [Fact]
    public async Task Stop_WithMessageInFlight_FinishesItAndKeepsTheSession()
    {
        var clientId = NewName();
        var intake = _fixture.CreateIntake(NewName(), clientId);
        await intake.Service.StartAsync(CancellationToken.None);
        await MqttDrainFixture.WaitUntilConnectedAsync(intake);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        _fixture.SlowSave.Arm(SlowSaveDelay);
        await _fixture.PublishAllAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId),
            Payloads(offset: 0, count: 1));
        await WaitUntilSaveStartedAsync();

        await intake.Service.StopAsync(CancellationToken.None);

        var stored = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);
        using var client = _fixture.CreateClient();
        var reconnect = await _fixture.ReconnectAsync(ConsumerClientId(clientId, index: 0), client);

        stored.Should().ContainSingle();
        reconnect.IsSessionPresent.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_WithMessageAfterStopSignal_StoresItAfterTheRestart()
    {
        var group = NewName();
        var clientId = NewName();
        var first = _fixture.CreateIntake(group, clientId);
        await first.Service.StartAsync(CancellationToken.None);
        await MqttDrainFixture.WaitUntilConnectedAsync(first);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        _fixture.SlowSave.Arm(SlowSaveDelay);
        await _fixture.PublishAllAsync(topic, Payloads(offset: 0, count: 1));
        await WaitUntilSaveStartedAsync();
        await _fixture.PublishAllAsync(topic, Payloads(offset: 1, count: 1));
        await Task.Delay(IntakeTestData.QuietPeriod);
        await first.Service.StopAsync(CancellationToken.None);
        var storedAfterStop = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);
        var second = _fixture.CreateIntake(group, clientId);

        await second.Service.StartAsync(CancellationToken.None);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 2);
        await second.Service.StopAsync(CancellationToken.None);

        storedAfterStop.Should().ContainSingle();
        messages.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Stop_WhenDrainTimesOut_KeepsTheSession()
    {
        var clientId = NewName();
        var intake = _fixture.CreateIntake(NewName(), clientId, drainTimeoutSeconds: 1);
        await intake.Service.StartAsync(CancellationToken.None);
        await MqttDrainFixture.WaitUntilConnectedAsync(intake);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var payload = Payloads(offset: 0, count: 1).Single();
        using var timeouts = Collect(Constants.Intake.Instruments.DrainTimeouts);
        using var messages = Collect(Constants.Intake.Instruments.Messages);
        _fixture.SlowSave.Arm(VerySlowSaveDelay);
        await _fixture.PublishAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);
        await WaitUntilSaveStartedAsync();

        await intake.Service.StopAsync(CancellationToken.None);

        var stored = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);
        var redelivered = new List<byte[]>();
        using var client = _fixture.CreateClient();
        client.ApplicationMessageReceivedAsync += arguments =>
        {
            redelivered.Add(arguments.ApplicationMessage.Payload.ToArray());

            return Task.CompletedTask;
        };
        var reconnect = await _fixture.ReconnectAsync(ConsumerClientId(clientId, index: 0), client);
        await Task.Delay(IntakeTestData.QuietPeriod);

        stored.Should().BeEmpty();
        reconnect.IsSessionPresent.Should().BeTrue();
        redelivered.Should().ContainSingle().Which.Should().Equal(payload);
        timeouts.GetMeasurementSnapshot().Sum(measurement => measurement.Value).Should().Be(expected: 1);
        messages.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WithLowerConsumerCount_StoresTheQueuedMessagesOfTheOrphan()
    {
        var group = NewName();
        var clientId = NewName();
        var orphanId = ConsumerClientId(clientId, index: 1);
        await _fixture.CreateSessionAsync(orphanId, group);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        await _fixture.PublishAllAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId),
            Payloads(offset: 0, IntakeTestData.MessageCount));
        var intake = _fixture.CreateIntake(group, clientId);
        using var emptied = Collect(Constants.Intake.Instruments.OrphanEmptied);

        await intake.Service.StartAsync(CancellationToken.None);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, IntakeTestData.MessageCount);
        await intake.Service.OrphanCheckCompleted;
        await intake.Service.StopAsync(CancellationToken.None);
        using var client = _fixture.CreateClient();
        var reconnect = await _fixture.ReconnectAsync(orphanId, client);

        messages.Should().HaveCount(IntakeTestData.MessageCount);
        reconnect.IsSessionPresent.Should().BeFalse();
        emptied.GetMeasurementSnapshot().Sum(measurement => measurement.Value).Should().Be(IntakeTestData.MessageCount);
    }

    [Fact]
    public async Task Start_WithTransientFailureForOrphan_KeepsTheSessionAndStoresTheMessageAtTheNextStart()
    {
        var group = NewName();
        var clientId = NewName();
        var orphanId = ConsumerClientId(clientId, index: 1);
        await _fixture.CreateSessionAsync(orphanId, group);
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        await _fixture.PublishAllAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId),
            Payloads(offset: 0, count: 1));
        _fixture.SaveFailure.Arm(new TimeoutException());
        var first = _fixture.CreateIntake(group, clientId);
        await first.Service.StartAsync(CancellationToken.None);
        await first.Service.OrphanCheckCompleted;
        await first.Service.StopAsync(CancellationToken.None);
        var storedAfterFailure = await _fixture.Database.LoadReceivedMessagesAsync(machine.MachineId);
        using var probe = _fixture.CreateClient();
        var reconnect = await _fixture.ReconnectAsync(orphanId, probe);
        await probe.DisconnectAsync(new MqttClientDisconnectOptions());
        var second = _fixture.CreateIntake(group, clientId);

        await second.Service.StartAsync(CancellationToken.None);

        var messages = await _fixture.WaitForMessagesAsync(machine.MachineId, expectedCount: 1);
        await second.Service.OrphanCheckCompleted;
        await second.Service.StopAsync(CancellationToken.None);

        storedAfterFailure.Should().BeEmpty();
        reconnect.IsSessionPresent.Should().BeTrue();
        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task Start_WithoutOrphans_CreatesNoSession()
    {
        var clientId = NewName();
        var intake = _fixture.CreateIntake(NewName(), clientId);

        await intake.Service.StartAsync(CancellationToken.None);

        await intake.Service.OrphanCheckCompleted;
        await intake.Service.StopAsync(CancellationToken.None);
        var presentIndexes = new List<int>();

        for (var index = 1; index < MqttIntakeConstants.Limits.MaxConsumerCount; index++)
        {
            using var client = _fixture.CreateClient();
            var reconnect = await _fixture.ReconnectAsync(ConsumerClientId(clientId, index), client);

            if (reconnect.IsSessionPresent)
            {
                presentIndexes.Add(index);
            }
        }

        presentIndexes.Should().BeEmpty();
    }

    private MetricCollector<long> Collect(string instrument) =>
        new(_fixture.MeterFactory, Constants.Intake.Meter, instrument);

    private static string NewName() => Guid.NewGuid().ToString(format: "N");

    private static string ConsumerClientId(string clientId, int index) =>
        string.Format(MqttIntakeConstants.ConsumerClientIdFormat, clientId, index);

    private static List<byte[]> Payloads(int offset, int count) =>
        Enumerable.Range(offset, count)
            .Select(
                index => ReceiveMachineMessageTestData.Bytes(
                    ReceiveMachineMessageTestData.CreateMessage(
                        ReceiveMachineMessageTestData.SampledAt.AddSeconds(index))))
            .ToList();

    private async Task WaitUntilSaveStartedAsync()
    {
        var deadline = DateTime.UtcNow + IntakeTestData.WaitLimit;

        while (!_fixture.SlowSave.HasStarted && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50));
        }
    }
}
