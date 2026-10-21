using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.UnitTests.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class InboxTests
{
    [Fact]
    public void MarkProcessed_WithMessageTrackedByScopedContext_SetsProcessedOnAndMarksModified()
    {
        using var provider = MessagingTestServices.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        var message = MessagingTestData.CreateInboxMessage();
        dbContext.InboxMessages.Attach(message);

        scope.ServiceProvider.GetRequiredService<IInbox>().MarkProcessed(message);

        message.ProcessedOn.Should().Be(MessagingTestData.Now);
        dbContext.Entry(message).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public void MarkProcessed_WithUntrackedMessage_ThrowsInvalidOperationException()
    {
        using var provider = MessagingTestServices.Build();
        using var scope = provider.CreateScope();
        var message = MessagingTestData.CreateInboxMessage();

        var act = () => scope.ServiceProvider.GetRequiredService<IInbox>().MarkProcessed(message);

        act.Should().Throw<InvalidOperationException>();
        message.ProcessedOn.Should().BeNull();
    }

    [Fact]
    public void MarkProcessed_WithAlreadyProcessedMessage_ThrowsInvalidOperationException()
    {
        var message = MessagingTestData.CreateInboxMessage();
        message.MarkProcessed(MessagingTestData.EarlierProcessedOn);

        var act = () => message.MarkProcessed(MessagingTestData.Now);

        act.Should().Throw<InvalidOperationException>();
        message.ProcessedOn.Should().Be(MessagingTestData.EarlierProcessedOn);
    }
}
