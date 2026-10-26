using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed class TestAuditedEntity : IEntity, IAuditable
{
    public TestAuditedEntity(string name)
    {
        Name = name;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }
}

public sealed class TestAuditedEntityConfiguration : IEntityTypeConfiguration<TestAuditedEntity>
{
    public void Configure(EntityTypeBuilder<TestAuditedEntity> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
    }
}

public sealed record TestRecorded : IIntegrationMessage
{
    public static string MessageType => InboxProcessorTestData.RecordedMessageType;

    public required string Text { get; init; }
}

public sealed record TestFailing : IIntegrationMessage
{
    public static string MessageType => InboxProcessorTestData.FailingMessageType;

    public required string Text { get; init; }
}

public sealed record HandledCall(string Text, ActorContext? Actor);

/// <summary>
/// Records each call of the test handlers, so a test sees how often the handler ran and as which actor.
/// </summary>
public sealed class HandlerRecorder
{
    private readonly List<HandledCall> _calls = [];

    public List<HandledCall> CallsFor(string text)
    {
        lock (_calls)
        {
            return _calls.Where(call => call.Text == text).ToList();
        }
    }

    public void Record(HandledCall call)
    {
        lock (_calls)
        {
            _calls.Add(call);
        }
    }
}

public sealed class RecordingHandler(
    ITestServiceDbContext dbContext,
    ICurrentUserProvider currentUserProvider,
    HandlerRecorder recorder
) : IIntegrationMessageHandler<TestRecorded>
{
    public async Task HandleAsync(TestRecorded message, CancellationToken cancellationToken)
    {
        dbContext.AuditedEntities.Add(new TestAuditedEntity(message.Text));
        await dbContext.SaveChangesAsync(cancellationToken);

        recorder.Record(new HandledCall(message.Text, currentUserProvider.TryGetActor()));
    }
}

public sealed class FailingHandler(ITestServiceDbContext dbContext) : IIntegrationMessageHandler<TestFailing>
{
    public async Task HandleAsync(TestFailing message, CancellationToken cancellationToken)
    {
        dbContext.AuditedEntities.Add(new TestAuditedEntity(message.Text));
        await dbContext.SaveChangesAsync(cancellationToken);

        throw new InvalidOperationException(InboxProcessorTestData.HandlerFailureText);
    }
}
