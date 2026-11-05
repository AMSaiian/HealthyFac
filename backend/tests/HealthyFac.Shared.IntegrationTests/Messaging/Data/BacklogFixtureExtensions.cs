using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class BacklogFixtureExtensions
{
    extension(PostgreSqlFixture fixture)
    {
        /// <summary>
        /// Adds pending outbox rows, one pending inbox row, and one failed inbox row. The failed row is not pending,
        /// because no processor claims it again.
        /// </summary>
        public async Task SeedBacklogAsync()
        {
            using var scope = fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
            for (var index = 0; index < MessagingMetricsTestData.PendingOutboxCount; index++)
            {
                dbContext.OutboxMessages.Add(
                    new OutboxMessage(
                        OutboxProcessorTestData.PublishedMessageType,
                        OutboxProcessorTestData.Payload,
                        MessagingMetricsTestData.OutboxOccurredOn,
                        null,
                        InboxTestData.ActorId,
                        InboxTestData.CompanyId));
            }

            dbContext.InboxMessages.Add(
                new InboxMessage(
                    Guid.CreateVersion7(),
                    InboxProcessorTestData.RecordedMessageType,
                    InboxProcessorTestData.PayloadFor(MessagingMetricsTestData.HandledText),
                    MessagingMetricsTestData.InboxReceivedOn,
                    null,
                    InboxTestData.ActorId,
                    InboxTestData.CompanyId));
            dbContext.InboxMessages.Add(
                InboxProcessorTestData.CreateFailedRow(
                    Guid.CreateVersion7(),
                    InboxProcessorTestData.FailingMessageType,
                    MessagingMetricsTestData.RetriedText,
                    MessagingMetricsTestData.InboxReceivedOn.AddHours(-1)));

            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Marks every pending outbox and inbox row processed, as the processors do.
        /// </summary>
        public async Task MarkBacklogProcessedAsync()
        {
            using var scope = fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
            var outboxRows = await dbContext.OutboxMessages.Where(row => row.ProcessedOn == null).ToListAsync();
            var inboxRows = await dbContext.InboxMessages
                .Where(row => row.ProcessedOn == null && row.FailedOn == null)
                .ToListAsync();
            outboxRows.ForEach(row => row.MarkPublished(InboxTestData.Now));
            inboxRows.ForEach(row => row.MarkProcessed(InboxTestData.Now));

            await dbContext.SaveChangesAsync();
        }
    }
}
