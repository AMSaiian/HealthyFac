using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class OutboxFixtureExtensions
{
    extension(TransportFixture fixture)
    {
        public async Task AddPendingOutboxRowsAsync(string messageType, int count)
        {
            using var scope = fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
            for (var index = 0; index < count; index++)
            {
                dbContext.OutboxMessages.Add(
                    new OutboxMessage(
                        messageType,
                        OutboxProcessorTestData.Payload,
                        InboxTestData.Now,
                        traceParent: null,
                        TransportTestData.ActorId,
                        TransportTestData.CompanyId));
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<List<OutboxMessage>> ReadOutboxRowsAsync(string messageType)
        {
            using var scope = fixture.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
                .OutboxMessages
                .AsNoTracking()
                .Where(message => message.Type == messageType)
                .ToListAsync();
        }
    }
}
