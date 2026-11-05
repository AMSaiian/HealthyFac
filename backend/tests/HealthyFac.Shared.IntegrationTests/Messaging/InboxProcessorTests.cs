using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxProcessorTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public async Task InboxProcessor_WithAnonymousActor_RestoresAnonymous()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.AnonymousRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.AnonymousActorText,
            KnownActors.Anonymous.UserId,
            null,
            InboxTestData.Now);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.AnonymousRowId);
        var calls = _fixture.Recorder.CallsFor(InboxProcessorTestData.AnonymousActorText);

        row.ProcessedOn.Should().Be(InboxTestData.Now);
        calls.Should().ContainSingle().Which.Actor!.Kind.Should().Be(ActorKind.Anonymous);
    }

    [Fact]
    public async Task InboxProcessor_WithActorHeaders_AuditsWithOriginalActor()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.UserRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.UserActorText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var entities = await _fixture.ReadAuditedEntitiesAsync(InboxProcessorTestData.UserActorText);
        var calls = _fixture.Recorder.CallsFor(InboxProcessorTestData.UserActorText);

        entities.Should().ContainSingle().Which.CreatedBy.Should().Be(InboxProcessorTestData.UserActorId);
        var actor = calls.Should().ContainSingle().Which.Actor!;
        actor.UserId.Should().Be(InboxProcessorTestData.UserActorId);
        actor.CompanyId.Should().Be(InboxProcessorTestData.UserCompanyId);
        actor.Kind.Should().Be(ActorKind.User);
    }

    [Fact]
    public async Task Process_WithInvalidPayload_FailsAtFirstAttempt()
    {
        await _fixture.AddPingInboxRowAsync(
            InboxProcessorTestData.InvalidPayloadRowId,
            InboxProcessorTestData.InvalidPayload);
        var callsBefore = _fixture.Recorder.Count;
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.InvalidPayloadRowId);
        var errors = _fixture.ReadErrorLogs(InboxProcessorTestData.InvalidPayloadRowId);

        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        _fixture.Recorder.Count.Should().Be(callsBefore);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Process_WithUnreadablePayload_FailsAtFirstAttempt()
    {
        await _fixture.AddPingInboxRowAsync(
            InboxProcessorTestData.UnreadablePayloadRowId,
            InboxProcessorTestData.UnreadablePayload);
        var callsBefore = _fixture.Recorder.Count;
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.UnreadablePayloadRowId);
        var errors = _fixture.ReadErrorLogs(InboxProcessorTestData.UnreadablePayloadRowId);

        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        _fixture.Recorder.Count.Should().Be(callsBefore);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Process_WithControlCharacter_FailsAtFirstAttempt()
    {
        await _fixture.AddPingInboxRowAsync(
            InboxProcessorTestData.ControlCharacterRowId,
            InboxProcessorTestData.ControlCharacterPayload);
        var callsBefore = _fixture.Recorder.Count;
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.ControlCharacterRowId);
        var errors = _fixture.ReadErrorLogs(InboxProcessorTestData.ControlCharacterRowId);

        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        _fixture.Recorder.Count.Should().Be(callsBefore);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task InboxProcessor_WhenHandlerThrows_RollsBackAndSchedulesRetry()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.FailingRowId,
            InboxProcessorTestData.FailingMessageType,
            InboxProcessorTestData.FailingText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.FailingRowId);
        var entities = await _fixture.ReadAuditedEntitiesAsync(InboxProcessorTestData.FailingText);

        entities.Should().BeEmpty();
        row.ProcessedOn.Should().BeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        row.Error.Should().Contain(InboxProcessorTestData.HandlerFailureText);
        row.NextAttemptOn.Should().BeAfter(InboxTestData.Now);
    }

    [Fact]
    public async Task InboxProcessor_AtAttemptLimit_MarksFailedAndLogsError()
    {
        await _fixture.AddInboxRowAsync(
            InboxProcessorTestData.CreateRowWithAttempts(
                InboxProcessorTestData.AtLimitRowId,
                InboxProcessorTestData.FailingMessageType,
                InboxProcessorTestData.AtLimitText,
                InboxTestData.Now,
                InboxProcessorTestData.MaxAttemptCount - 1));
        var logger = new CapturingLogger<InboxProcessor>();
        var processor = _fixture.Create<InboxProcessor>(logger);

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.AtLimitRowId);
        var errors = logger.Entries.Where(entry => entry.Level == LogLevel.Error).ToList();

        row.FailedOn.Should().Be(InboxTestData.Now);
        row.ProcessedOn.Should().BeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.MaxAttemptCount);
        errors.Should()
            .ContainSingle()
            .Which.Properties["MessageId"]
            .Should()
            .Be(InboxProcessorTestData.AtLimitRowId);
    }

    [Fact]
    public async Task InboxProcessor_WithFailedRow_SkipsItAndHandlesNext()
    {
        await _fixture.AddInboxRowAsync(
            InboxProcessorTestData.CreateFailedRow(
                InboxProcessorTestData.FailedRowId,
                InboxProcessorTestData.RecordedMessageType,
                InboxProcessorTestData.FailedRowText,
                InboxTestData.Now.AddSeconds(-2)));
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.ValidAfterFailedRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.ValidAfterFailedText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-1));
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var failedRow = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.FailedRowId);
        var validRow = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.ValidAfterFailedRowId);

        failedRow.ProcessedOn.Should().BeNull();
        failedRow.FailedOn.Should().NotBeNull();
        failedRow.AttemptCount.Should().Be(1);
        validRow.ProcessedOn.Should().Be(InboxTestData.Now);
    }

    [Fact]
    public async Task InboxProcessor_WithUnregisteredType_SchedulesRetryAndContinues()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.UnregisteredRowId,
            InboxProcessorTestData.UnregisteredMessageType,
            InboxProcessorTestData.ValidAfterUnregisteredText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-2));
        await _fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.ValidAfterUnregisteredRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.ValidAfterUnregisteredText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-1));
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var unregisteredRow = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.UnregisteredRowId);
        var validRow = await _fixture.ReadInboxRowAsync(InboxProcessorTestData.ValidAfterUnregisteredRowId);

        unregisteredRow.ProcessedOn.Should().BeNull();
        unregisteredRow.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        unregisteredRow.Error.Should().Contain(InboxProcessorTestData.UnregisteredMessageType);
        validRow.ProcessedOn.Should().Be(InboxTestData.Now);
    }
}
