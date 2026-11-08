using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Application.Intake;
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

        var response = await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

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

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
        response.SampledAt.Should().Be(ReceiveMachineMessageTestData.SampledAt);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.OutOfRangeTemperatures),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithNumberOutsideRange_StoresFlaggedReadingWithoutError(double temperature)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: temperature);

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().BeEmpty();
        readings.Numeric.Single().IsOutOfRange.Should().BeTrue();

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.RangeEdgeTemperatures),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithNumberOnTheRangeEdge_StoresReadingInRange(double temperature)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: temperature);

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        readings.Numeric.Single().IsOutOfRange.Should().BeFalse();

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
    }

    [Fact]
    public async Task Receive_WithStateOutsideSet_StoresFlaggedReadingWithoutError()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(mode: "unknown");

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().BeEmpty();
        readings.Discrete.Single(reading => reading.Value == "unknown").IsOutOfRange.Should().BeTrue();
        readings.Discrete.Single(reading => reading.Value == "true").IsOutOfRange.Should().BeFalse();

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
    }

    [Fact]
    public async Task Receive_WithWrongType_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(temperature: "hot");

        var response = await _fixture.ReceiveAsync(
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

        response.Result.Should().Be(ReceiveMachineMessageResult.StoredWithErrors);
    }

    [Fact]
    public async Task Receive_WithUnknownMetricKey_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage();
        message[propertyName: "metrics"]![propertyName: "extra.key"] = 1;

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownMetric);
        readings.Numeric.Should().ContainSingle();
        readings.Discrete.Should().HaveCount(expected: 2);

        response.Result.Should().Be(ReceiveMachineMessageResult.StoredWithErrors);
    }

    [Fact]
    public async Task Receive_WithMissingMetricKey_StoresErrorAndTheOtherValues()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage();
        message[propertyName: "metrics"]!.AsObject().Remove(ReceiveMachineMessageTestData.DoorKey);

        var response = await _fixture.ReceiveAsync(
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

        response.Result.Should().Be(ReceiveMachineMessageResult.StoredWithErrors);
    }

    [Fact]
    public async Task Receive_WithUnknownStatusCode_StoresErrorAndTheKnownStatus()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            statuses: [ReceiveMachineMessageTestData.ErrorStatus, ReceiveMachineMessageTestData.UnknownStatus]);

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownStatus);
        readings.Statuses.Should().ContainSingle();

        response.Result.Should().Be(ReceiveMachineMessageResult.StoredWithErrors);
    }

    [Fact]
    public async Task Receive_WithStatusTwice_StoresOneReadingAndError()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            statuses: [ReceiveMachineMessageTestData.ErrorStatus, ReceiveMachineMessageTestData.ErrorStatus]);

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.DuplicateStatus);
        readings.Statuses.Should().ContainSingle();

        response.Result.Should().Be(ReceiveMachineMessageResult.StoredWithErrors);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.EnvelopeFailures), MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithEnvelopeFailure_StoresErrorAndNoReadings(string json, string expectedCode)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(json));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Select(error => error.Code).Should().Contain(expectedCode);
        readings.Total.Should().Be(expected: 0);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(expectedCode);
    }

    [Fact]
    public async Task Receive_WithSampledAtOneMinuteAhead_IsValid()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            ReceiveMachineMessageTestData.ReceivedAt.AddMinutes(value: 1));

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().BeEmpty();

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
    }

    [Fact]
    public async Task Receive_WithSampledAtJustAboveOneMinuteAhead_IsEnvelopeFailure()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var message = ReceiveMachineMessageTestData.CreateMessage(
            ReceiveMachineMessageTestData.ReceivedAt.AddMinutes(value: 1).AddTicks(value: 1));

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), ReceiveMachineMessageTestData.Bytes(message));

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.FutureSampledAt);
        readings.Total.Should().Be(expected: 0);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.FutureSampledAt);
    }

    [Fact]
    public async Task Receive_WithNotJson_StoresMalformedJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(text: "not json at all");

        var response = await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task Receive_WithInvalidUtf8_StoresMalformedJson()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();

        var response = await _fixture.ReceiveAsync(
            ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), [0xFF, 0xFE, 0x7B]);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task Receive_WithUnicodeNullEscape_StoresUnstorableJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(text: """{"a":"x\u0000y"}""");

        var response = await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnstorableJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.UnstorableJson);
    }

    [Fact]
    public async Task Receive_WithDuplicateMetricKey_StoresMalformedJsonWithRawBytes()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.DuplicateMetricKeyMessage);

        var response = await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();
        var readings = await _fixture.LoadReadingsAsync(machine.MachineId);

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);
        readings.Numeric.Should().BeEmpty();

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.MalformedJson);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.LoneSurrogateMessages),
        MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithLoneSurrogateEscape_StoresUnstorableJsonWithRawBytes(string json)
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(json);

        var response = await _fixture.ReceiveAsync(ReceiveMachineMessageTestData.TopicFor(machine.GatewayId), payload);

        var stored = (await _fixture.LoadReceivedMessagesAsync(machine.MachineId)).Single();

        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnstorableJson);
        stored.Payload.Should().BeNull();
        stored.RawPayload.Should().Equal(payload);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.UnstorableJson);
    }

    [Theory]
    [MemberData(
        nameof(ReceiveMachineMessageTestData.MalformedTopics), MemberType = typeof(ReceiveMachineMessageTestData))]
    public async Task Receive_WithMalformedGatewayIdInTopic_StoresErrorWithoutMachine(string topic)
    {
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        var response = await _fixture.ReceiveAsync(topic, payload);

        var stored = (await _fixture.LoadReceivedMessagesByTopicAsync(topic)).First();

        stored.MachineRegistrationId.Should().BeNull();
        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.MalformedGatewayId);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.MalformedGatewayId);
    }

    [Fact]
    public async Task Receive_ForUnknownGateway_StoresErrorWithoutMachineOrReadings()
    {
        var topic = ReceiveMachineMessageTestData.TopicFor(ReceiveMachineMessageTestData.UnknownGatewayId);
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());

        var response = await _fixture.ReceiveAsync(topic, payload);

        var stored = (await _fixture.LoadReceivedMessagesByTopicAsync(topic)).First();

        stored.MachineRegistrationId.Should().BeNull();
        stored.Errors.Should().ContainSingle().Which.Code.Should().Be(IntakeErrorCodes.UnknownGateway);

        response.Result.Should().Be(ReceiveMachineMessageResult.Rejected);
        response.ErrorCode.Should().Be(IntakeErrorCodes.UnknownGateway);
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
    public async Task Receive_SameMessageTwice_ReturnsDuplicateTheSecondTime()
    {
        var machine = await _fixture.ArrangeIntakeMachineAsync();
        var payload = ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage());
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var first = await _fixture.ReceiveAsync(topic, payload);

        var second = await _fixture.ReceiveAsync(topic, payload);

        first.Result.Should().Be(ReceiveMachineMessageResult.Stored);
        second.Result.Should().Be(ReceiveMachineMessageResult.Duplicate);
        second.SampledAt.Should().Be(ReceiveMachineMessageTestData.SampledAt);
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
