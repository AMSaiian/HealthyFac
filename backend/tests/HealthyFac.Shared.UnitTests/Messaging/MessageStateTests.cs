using HealthyFac.Shared.UnitTests.Messaging.Data;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class MessageStateTests
{
    [Fact]
    public void OutboxScheduleRetry_ThreeTimes_UsesExponentialDelayCappedAt300Seconds()
    {
        var message = MessagingTestData.CreateOutboxMessage();

        var firstDelays = MessagingTestData.ScheduleRetries(message, 3);

        var cappedDelays = MessagingTestData.ScheduleRetries(message, MessagingTestData.CappedRetryCount);

        firstDelays.Should().Equal(MessagingTestData.ExpectedFirstDelays);
        cappedDelays.Last().Should().Be(TimeSpan.FromSeconds(MessagingTestData.MaxRetryDelaySeconds));
        message.AttemptCount.Should().Be(3 + MessagingTestData.CappedRetryCount);
    }

    [Fact]
    public void InboxRecordFailure_ThreeTimes_UsesExponentialDelayCappedAt300Seconds()
    {
        var message = MessagingTestData.CreateInboxMessage();

        var firstDelays = MessagingTestData.RecordFailures(message, 3);

        var cappedDelays = MessagingTestData.RecordFailures(message, MessagingTestData.CappedRetryCount);

        firstDelays.Should().Equal(MessagingTestData.ExpectedFirstDelays);
        cappedDelays.Last().Should().Be(TimeSpan.FromSeconds(MessagingTestData.MaxRetryDelaySeconds));
        message.AttemptCount.Should().Be(3 + MessagingTestData.CappedRetryCount);
    }

    [Fact]
    public void OutboxScheduleRetry_WithLongError_TruncatesTo2000Characters()
    {
        var message = MessagingTestData.CreateOutboxMessage();

        message.ScheduleRetry(MessagingTestData.Now, new string('x', MessagingTestData.ErrorMaxLength + 50));

        message.Error.Should().HaveLength(MessagingTestData.ErrorMaxLength);
    }

    [Fact]
    public void InboxRecordFailure_WithLongError_TruncatesTo2000Characters()
    {
        var message = MessagingTestData.CreateInboxMessage();

        message.RecordFailure(
            MessagingTestData.Now,
            new string('x', MessagingTestData.ErrorMaxLength + 50),
            MessagingTestData.HighAttemptLimit);

        message.Error.Should().HaveLength(MessagingTestData.ErrorMaxLength);
    }

    [Fact]
    public void OutboxScheduleRetry_AfterPublished_Throws()
    {
        var message = MessagingTestData.CreateOutboxMessage();
        message.MarkPublished(MessagingTestData.EarlierProcessedOn);

        var act = () => message.ScheduleRetry(MessagingTestData.Now, MessagingTestData.RetryError);

        act.Should().Throw<InvalidOperationException>();
        message.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void InboxRecordFailure_AfterProcessed_Throws()
    {
        var message = MessagingTestData.CreateInboxMessage();
        message.MarkProcessed(MessagingTestData.EarlierProcessedOn);

        var act = () => message.RecordFailure(
            MessagingTestData.Now,
            MessagingTestData.RetryError,
            MessagingTestData.HighAttemptLimit);

        act.Should().Throw<InvalidOperationException>();
        message.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void RecordFailure_BelowLimit_SchedulesRetry()
    {
        var message = MessagingTestData.CreateInboxMessage();

        message.RecordFailure(MessagingTestData.Now, MessagingTestData.RetryError, MessagingTestData.ThreeAttemptLimit);

        message.AttemptCount.Should().Be(1);
        message.FailedOn.Should().BeNull();
        message.NextAttemptOn.Should().Be(MessagingTestData.Now.AddSeconds(2));
    }

    [Fact]
    public void RecordFailure_AtLimit_SetsFailedOnAndKeepsNextAttemptOn()
    {
        var message = MessagingTestData.CreateInboxMessage();
        var nextAttemptOn = message.NextAttemptOn;

        message.RecordFailure(
            MessagingTestData.Now,
            MessagingTestData.RetryError,
            MessagingTestData.SingleAttemptLimit);

        message.AttemptCount.Should().Be(1);
        message.FailedOn.Should().Be(MessagingTestData.Now);
        message.NextAttemptOn.Should().Be(nextAttemptOn);
    }

    [Fact]
    public void RecordFailure_AfterFailed_Throws()
    {
        var message = MessagingTestData.CreateInboxMessage();
        message.RecordFailure(
            MessagingTestData.Now,
            MessagingTestData.RetryError,
            MessagingTestData.SingleAttemptLimit);

        var act = () => message.RecordFailure(
            MessagingTestData.Now,
            MessagingTestData.RetryError,
            MessagingTestData.SingleAttemptLimit);

        act.Should().Throw<InvalidOperationException>();
        message.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void InboxMarkProcessed_AfterFailed_Throws()
    {
        var message = MessagingTestData.CreateInboxMessage();
        message.RecordFailure(
            MessagingTestData.Now,
            MessagingTestData.RetryError,
            MessagingTestData.SingleAttemptLimit);

        var act = () => message.MarkProcessed(MessagingTestData.Now);

        act.Should().Throw<InvalidOperationException>();
        message.ProcessedOn.Should().BeNull();
    }

    [Fact]
    public void OutboxMarkPublished_AfterPublished_Throws()
    {
        var message = MessagingTestData.CreateOutboxMessage();
        message.MarkPublished(MessagingTestData.EarlierProcessedOn);

        var act = () => message.MarkPublished(MessagingTestData.Now);

        act.Should().Throw<InvalidOperationException>();
        message.ProcessedOn.Should().Be(MessagingTestData.EarlierProcessedOn);
    }
}
