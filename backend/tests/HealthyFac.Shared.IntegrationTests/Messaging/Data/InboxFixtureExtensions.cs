using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class InboxFixtureExtensions
{
    extension(TransportFixture fixture)
    {
        public async Task AddPendingInboxRowAsync(
            Guid messageId,
            string messageType,
            string text,
            Guid actorId,
            Guid? companyId,
            DateTime receivedOn
        )
        {
            using var scope = fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
            dbContext.InboxMessages.Add(
                new InboxMessage(
                    messageId,
                    messageType,
                    InboxProcessorTestData.PayloadFor(text),
                    receivedOn,
                    traceParent: null,
                    actorId,
                    companyId));

            await dbContext.SaveChangesAsync();
        }

        public Task AddPingInboxRowAsync(Guid messageId, string payload) => fixture.AddInboxRowAsync(
            new InboxMessage(
                messageId,
                TransportTestData.PingMessageType,
                payload,
                InboxTestData.Now,
                traceParent: null,
                InboxProcessorTestData.UserActorId,
                InboxProcessorTestData.UserCompanyId));

        public async Task AddInboxRowAsync(InboxMessage row)
        {
            using var scope = fixture.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
            dbContext.InboxMessages.Add(row);

            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Waits until the handler ran for the text, then waits a quiet period, so a second call would show in the
        /// result.
        /// </summary>
        public async Task<List<HandledCall>> WaitForHandledCallsAsync(string text)
        {
            using var cancellation = new CancellationTokenSource(TransportTestData.WaitTimeout);
            while (!cancellation.IsCancellationRequested && fixture.Recorder.CallsFor(text).Count == 0)
            {
                await Task.Delay(TransportTestData.PollDelay);
            }

            await Task.Delay(TransportTestData.QuietPeriod);

            return fixture.Recorder.CallsFor(text);
        }

        public async Task<InboxMessage> ReadInboxRowAsync(Guid messageId)
        {
            using var scope = fixture.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(message => message.Id == messageId);
        }

        public async Task<List<TestAuditedEntity>> ReadAuditedEntitiesAsync(string name)
        {
            using var scope = fixture.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
                .AuditedEntities
                .AsNoTracking()
                .Where(entity => entity.Name == name)
                .ToListAsync();
        }
    }
}
