using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

public sealed class ReceiveMachineMessageTests(TelemetryDatabaseFixture fixture)
    : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Receive_WithValidMessage_StoresMessageAndReadings()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var messages = await _fixture.LoadReceivedMessagesAsync(machine.MachineId);
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);
        var message = messages.Single();

        message.Errors.Should().BeEmpty();
        message.SampledAt.Should().Be(ReceiveMachineMessageTestData.SampledAt);
        message.ReceivedAt.Should().Be(ReceiveMachineMessageTestData.ReceivedAt);
        message.SchemaRevision.Should().Be(expected: 1);
        message.RawPayload.Should().BeNull();
        message.Payload.Should().NotBeNull();
        readings.Numeric.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { Value = 71.5, IsOutOfRange = false });
        readings.Discrete.Select(reading => reading.Value).Should().BeEquivalentTo("true", "auto");
        readings.Discrete.Should().OnlyContain(reading => !reading.IsOutOfRange);
        readings.Statuses.Should().HaveCount(expected: 2);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.OutOfRangeTemperatures),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithNumberOutsideRange_StoresFlaggedReadingWithoutError(double temperature)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: temperature);

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().BeEmpty();
        readings.Numeric.Single().IsOutOfRange.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.RangeEdgeTemperatures),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithNumberOnTheRangeEdge_StoresReadingInRange(double temperature)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: temperature);

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        readings.Numeric.Single().IsOutOfRange.Should().BeFalse();
    }

    [Fact]
    public async Task Receive_WithStateOutsideSet_StoresFlaggedReadingWithoutError()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(mode: "unknown");

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().BeEmpty();
        readings.Discrete.Single(reading => reading.Value == "unknown").IsOutOfRange.Should().BeTrue();
        readings.Discrete.Single(reading => reading.Value == "true").IsOutOfRange.Should().BeFalse();
    }

    [Fact]
    public async Task Receive_WithWrongType_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: "hot");

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { Path = "metrics.spindle.temperature", Code = IntakeErrorCodes.WrongType });
        readings.Numeric.Should().BeEmpty();
        readings.Discrete.Should().HaveCount(expected: 2);
        readings.Statuses.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Receive_WithUnknownMetricKey_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage();
        message[propertyName: "metrics"]![propertyName: "extra.key"] = 1;

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownMetric);
        readings.Numeric.Should().ContainSingle();
        readings.Discrete.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Receive_WithMissingMetricKey_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage();
        message[propertyName: "metrics"]!.AsObject().Remove(ReceiveMachineMessageTestData.DoorKey);

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { Path = "metrics.door.open", Code = IntakeErrorCodes.MissingMetric });
        readings.Numeric.Should().ContainSingle();
        readings.Discrete.Should().ContainSingle();
    }

    [Fact]
    public async Task Receive_WithUnknownStatusCode_StoresErrorAndTheKnownStatus()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            statuses: [ReceiveMachineMessageTestData.ErrorStatus, ReceiveMachineMessageTestData.UnknownStatus]);

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownStatus);
        readings.Statuses.Should().ContainSingle();
    }

    [Fact]
    public async Task Receive_WithStatusTwice_StoresOneReadingAndError()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            statuses: [ReceiveMachineMessageTestData.ErrorStatus, ReceiveMachineMessageTestData.ErrorStatus]);

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.DuplicateStatus);
        readings.Statuses.Should().ContainSingle();
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.EnvelopeFailures), MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithEnvelopeFailure_StoresErrorAndNoReadings(string json, string expectedCode)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(json));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Select(error => error.Code).Should().Contain(expectedCode);
        readings.Total.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Receive_WithSampledAtOneMinuteAhead_IsValid()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            ReceiveMachineMessageTestData.ReceivedAt.AddMinutes(value: 1));

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Receive_WithSampledAtJustAboveOneMinuteAhead_IsEnvelopeFailure()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            ReceiveMachineMessageTestData.ReceivedAt.AddMinutes(value: 1).AddTicks(value: 1));

        await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.FutureSampledAt);
        readings.Total.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Receive_WithNotJson_StoresMalformedJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(text: "not json at all");

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);
    }

    [Fact]
    public async Task Receive_WithInvalidUtf8_StoresMalformedJson()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), [0xFF, 0xFE, 0x7B]);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task Receive_WithUnicodeNullEscape_StoresUnstorableJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(text: """{"a":"x\u0000y"}""");

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnstorableJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);
    }

    [Fact]
    public async Task Receive_WithDuplicateMetricKey_StoresMalformedJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.DuplicateMetricKeyMessage);

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);
        readings.Numeric.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.LoneSurrogateMessages),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithLoneSurrogateEscape_StoresUnstorableJsonWithRawBytes(string json)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(json);

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnstorableJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.MalformedTopics), MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithMalformedGatewayIdInTopic_StoresErrorWithoutMachine(string topic)
    {
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        await _fixture.ReceiveAsync(topic, payload);

        var stored = (await _fixture.LoadReceivedMessagesByTopicAsync(topic)).First();

        stored.MachineRegistrationId.Should().BeNull();
        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedGatewayId);
    }

    [Fact]
    public async Task Receive_ForUnknownGateway_StoresErrorWithoutMachineOrReadings()
    {
        var topic = ReceiveMachineMessageTestData.TopicFor(ReceiveMachineMessageTestData.UnknownGatewayId);
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        await _fixture.ReceiveAsync(topic, payload);

        var stored = (await _fixture.LoadReceivedMessagesByTopicAsync(topic)).First();

        stored.MachineRegistrationId.Should().BeNull();
        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownGateway);
    }

    [Fact]
    public async Task Receive_SameMessageTwice_StoresOnce()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        await _fixture.ReceiveAsync(topic, payload);

        var act = () => _fixture.ReceiveAsync(topic, payload);

        await act.Should().NotThrowAsync();
        var messages = await _fixture.LoadReceivedMessagesAsync(machine.MachineId);
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        messages.Should().ContainSingle();
        readings.Total.Should().Be(expected: 5);
    }

    [Fact]
    public async Task Receive_SameMessageInParallel_StoresOnceAndBothSucceed()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var race = _fixture.SaveGate.StartRace(participantCount: 2);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ReceiveAsync(topic, payload)),
            race.RunAsync(() => _fixture.ReceiveAsync(topic, payload)));

        var messages = await _fixture.LoadReceivedMessagesAsync(machine.MachineId);
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Should().OnlyContain(failure => failure == null);
        messages.Should().ContainSingle();
        readings.Total.Should().Be(expected: 5);
    }

    [Fact]
    public async Task Receive_ForMachineWithoutDefinitions_AcceptsEmptyMetricsAndStatuses()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync(isWithDefinitions: false);
        var payload = ReceiveMachineMessageTestData.Bytes(
            new JsonObject
            {
                [propertyName: "sampledAt"] = ReceiveMachineMessageTestData.SampledAt.ToString(format: "O"),
                [propertyName: "metrics"] = new JsonObject(),
                [propertyName: "statuses"] = new JsonArray(),
            });

        await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().BeEmpty();
        stored.SchemaRevision.Should().Be(expected: 0);
    }
}
