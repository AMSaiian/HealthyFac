using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxProcessorTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public async Task InboxProcessor_WithRegisteredType_CallsHandlerAndMarksProcessed()
    {
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.SystemRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.SystemActorText,
            SystemActor.Id,
            companyId: null,
            InboxTestData.Now);
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await fixture.ReadInboxRowAsync(InboxProcessorTestData.SystemRowId);
        var calls = fixture.Recorder.CallsFor(InboxProcessorTestData.SystemActorText);

        row.ProcessedOn.Should().Be(InboxTestData.Now);
        calls.Should().ContainSingle().Which.Actor!.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task InboxProcessor_WithActorHeaders_AuditsWithOriginalActor()
    {
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.UserRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.UserActorText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var entities = await fixture.ReadAuditedEntitiesAsync(InboxProcessorTestData.UserActorText);
        var calls = fixture.Recorder.CallsFor(InboxProcessorTestData.UserActorText);

        entities.Should().ContainSingle().Which.CreatedBy.Should().Be(InboxProcessorTestData.UserActorId);
        var actor = calls.Should().ContainSingle().Which.Actor!;
        actor.UserId.Should().Be(InboxProcessorTestData.UserActorId);
        actor.CompanyId.Should().Be(InboxProcessorTestData.UserCompanyId);
        actor.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Process_WithInvalidPayload_FailsAtFirstAttempt()
    {
        await fixture.AddPingInboxRowAsync(
            InboxProcessorTestData.InvalidPayloadRowId,
            InboxProcessorTestData.InvalidPayload);
        var callsBefore = fixture.Recorder.Count;
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await fixture.ReadInboxRowAsync(InboxProcessorTestData.InvalidPayloadRowId);
        var errors = fixture.ReadErrorLogs(InboxProcessorTestData.InvalidPayloadRowId);

        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        fixture.Recorder.Count.Should().Be(callsBefore);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Process_WithUnreadablePayload_FailsAtFirstAttempt()
    {
        await fixture.AddPingInboxRowAsync(
            InboxProcessorTestData.UnreadablePayloadRowId,
            InboxProcessorTestData.UnreadablePayload);
        var callsBefore = fixture.Recorder.Count;
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await fixture.ReadInboxRowAsync(InboxProcessorTestData.UnreadablePayloadRowId);
        var errors = fixture.ReadErrorLogs(InboxProcessorTestData.UnreadablePayloadRowId);

        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        fixture.Recorder.Count.Should().Be(callsBefore);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task InboxProcessor_WhenHandlerThrows_RollsBackAndSchedulesRetry()
    {
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.FailingRowId,
            InboxProcessorTestData.FailingMessageType,
            InboxProcessorTestData.FailingText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await fixture.ReadInboxRowAsync(InboxProcessorTestData.FailingRowId);
        var entities = await fixture.ReadAuditedEntitiesAsync(InboxProcessorTestData.FailingText);

        entities.Should().BeEmpty();
        row.ProcessedOn.Should().BeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        row.Error.Should().Contain(InboxProcessorTestData.HandlerFailureText);
        row.NextAttemptOn.Should().BeAfter(InboxTestData.Now);
    }

    [Fact]
    public async Task InboxProcessor_AtAttemptLimit_MarksFailedAndLogsError()
    {
        await fixture.AddInboxRowAsync(
            InboxProcessorTestData.CreateRowWithAttempts(
                InboxProcessorTestData.AtLimitRowId,
                InboxProcessorTestData.FailingMessageType,
                InboxProcessorTestData.AtLimitText,
                InboxTestData.Now,
                InboxProcessorTestData.MaxAttemptCount - 1));
        var logger = new CapturingLogger<InboxProcessor>();
        var processor = fixture.Create<InboxProcessor>(logger);

        await processor.RunAsync(CancellationToken.None);

        var row = await fixture.ReadInboxRowAsync(InboxProcessorTestData.AtLimitRowId);
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
        await fixture.AddInboxRowAsync(
            InboxProcessorTestData.CreateFailedRow(
                InboxProcessorTestData.FailedRowId,
                InboxProcessorTestData.RecordedMessageType,
                InboxProcessorTestData.FailedRowText,
                InboxTestData.Now.AddSeconds(-2)));
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.ValidAfterFailedRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.ValidAfterFailedText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-1));
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var failedRow = await fixture.ReadInboxRowAsync(InboxProcessorTestData.FailedRowId);
        var validRow = await fixture.ReadInboxRowAsync(InboxProcessorTestData.ValidAfterFailedRowId);

        failedRow.ProcessedOn.Should().BeNull();
        failedRow.FailedOn.Should().NotBeNull();
        failedRow.AttemptCount.Should().Be(1);
        validRow.ProcessedOn.Should().Be(InboxTestData.Now);
    }

    [Fact]
    public async Task InboxProcessor_WithUnregisteredType_SchedulesRetryAndContinues()
    {
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.UnregisteredRowId,
            InboxProcessorTestData.UnregisteredMessageType,
            InboxProcessorTestData.ValidAfterUnregisteredText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-2));
        await fixture.AddPendingInboxRowAsync(
            InboxProcessorTestData.ValidAfterUnregisteredRowId,
            InboxProcessorTestData.RecordedMessageType,
            InboxProcessorTestData.ValidAfterUnregisteredText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now.AddSeconds(-1));
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var unregisteredRow = await fixture.ReadInboxRowAsync(InboxProcessorTestData.UnregisteredRowId);
        var validRow = await fixture.ReadInboxRowAsync(InboxProcessorTestData.ValidAfterUnregisteredRowId);

        unregisteredRow.ProcessedOn.Should().BeNull();
        unregisteredRow.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
        unregisteredRow.Error.Should().Contain(InboxProcessorTestData.UnregisteredMessageType);
        validRow.ProcessedOn.Should().Be(InboxTestData.Now);
    }
}
