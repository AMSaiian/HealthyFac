using System.Text;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace HealthyFac.Shared.IntegrationTests.TestReset.Data;

public static class ResetFixtureExtensions
{
    extension(ResetFixture fixture)
    {
        public async Task AddRecordAsync(string name)
        {
            await using var dbContext = fixture.CreateDbContext();
            dbContext.RecordedItems.Add(new RecordedItem { Name = name });
            await dbContext.SaveChangesAsync();
        }

        public async Task<int> CountRecordsAsync()
        {
            await using var dbContext = fixture.CreateDbContext();

            return await dbContext.RecordedItems.CountAsync();
        }

        public async Task<int> CountReferenceItemsAsync()
        {
            await using var dbContext = fixture.CreateDbContext();

            return await dbContext.ReferenceItems.CountAsync();
        }

        public async Task<int> CountBaseItemsAsync()
        {
            await using var dbContext = fixture.CreateDbContext();

            return await dbContext.BaseItems.CountAsync();
        }

        public async Task PublishQueuedMessagesAsync()
        {
            await using var connection = await new ConnectionFactory { Uri = new Uri(fixture.BrokerConnectionString) }
                .CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true));

            for (var index = 0; index < ResetTestData.QueuedMessageCount; index++)
            {
                await channel.BasicPublishAsync(
                    string.Empty,
                    ResetTestData.QueueName,
                    Encoding.UTF8.GetBytes(ResetTestData.MessageText));
            }
        }

        public Task<int> CountQueuedMessagesAsync()
            => RabbitMqQueueReader.CountReadyMessagesAsync(fixture.BrokerConnectionString, ResetTestData.QueueName);
    }
}
