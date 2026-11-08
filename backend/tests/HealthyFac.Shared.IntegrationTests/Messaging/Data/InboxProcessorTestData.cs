using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class InboxProcessorTestData
{
    public const string RecordedMessageType = "test.v1:Recorded";
    public const string FailingMessageType = "test.v1:Failing";
    public const string UnregisteredMessageType = "test.v1:Unregistered";
    public const string HandlerFailureText = "handler failed on purpose";
    public const string AnonymousActorText = "anonymous actor message";
    public const string UserActorText = "user actor message";
    public const string FailingText = "failing message";
    public const string ValidAfterUnregisteredText = "valid after unregistered";
    public const string EndToEndText = "end to end message";
    public const string InvalidPayload = """{"text":"this text is longer than twenty characters"}""";
    public const string UnreadablePayload = """{"text":123}""";

    /// <summary>
    /// A control character that <c>jsonb</c> stores, so the row reaches the validator. A <c>\u0000</c> escape never
    /// reaches the inbox: <c>jsonb</c> rejects it at the insert.
    /// </summary>
    public const string ControlCharacterPayload = """{"text":"bell\u0007"}""";

    public const string LimitSql = "LIMIT";
    public const string ForUpdateSkipLockedSql = "FOR UPDATE SKIP LOCKED";
    public const string AtLimitText = "at attempt limit";
    public const string FailedRowText = "already failed";
    public const string ValidAfterFailedText = "valid after failed";
    public const string SeededError = "seeded failure";
    public const string QueueName = "limit-tests";
    public const int NoAttemptLimit = int.MaxValue;
    public const int OneCall = 1;
    public const int FailedAttemptCount = 1;

    public static readonly Guid UserActorId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000b1");
    public static readonly Guid UserCompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000d1");
    public static readonly Guid AnonymousRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000201");
    public static readonly Guid UserRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000202");
    public static readonly Guid FailingRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000203");
    public static readonly Guid UnregisteredRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000204");
    public static readonly Guid ValidAfterUnregisteredRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000205");

    public static readonly Guid AtLimitRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000206");
    public static readonly Guid FailedRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000207");
    public static readonly Guid ValidAfterFailedRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000208");
    public static readonly Guid InvalidPayloadRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000209");
    public static readonly Guid UnreadablePayloadRowId = Guid.Parse(input: "019a0000-0000-7000-8000-00000000020a");
    public static readonly Guid ControlCharacterRowId = Guid.Parse(input: "019a0000-0000-7000-8000-00000000020b");

    public static TheoryData<Guid, string> PermanentlyInvalidPayloads => new()
    {
        { InvalidPayloadRowId, InvalidPayload },
        { UnreadablePayloadRowId, UnreadablePayload },
        { ControlCharacterRowId, ControlCharacterPayload },
    };

    public static readonly int MaxAttemptCount =
        new MessagingOptions { QueueName = QueueName }.InboxMaxAttemptCount;

    public static InboxMessage CreateRowWithAttempts(
        Guid id,
        string messageType,
        string text,
        DateTime receivedOn,
        int attemptCount
    )
    {
        var row = new InboxMessage(
            id,
            messageType,
            PayloadFor(text),
            receivedOn,
            traceParent: null,
            UserActorId,
            UserCompanyId);
        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            row.RecordFailure(receivedOn.AddHours(value: -1), SeededError, NoAttemptLimit);
        }

        return row;
    }

    public static InboxMessage CreateFailedRow(Guid id, string messageType, string text, DateTime receivedOn)
    {
        var row = new InboxMessage(
            id,
            messageType,
            PayloadFor(text),
            receivedOn,
            traceParent: null,
            UserActorId,
            UserCompanyId);
        row.RecordFailure(receivedOn, SeededError, maxAttemptCount: 1);

        return row;
    }

    public static string PayloadFor(string text) => $$"""{"text":"{{text}}"}""";
}
