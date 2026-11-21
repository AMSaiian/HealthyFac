using System.Text.Json;
using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

public static class WorkOrderDraftFixtureExtensions
{
    private const int InboxWaitSteps = 200;
    private const int InboxWaitStepMilliseconds = 100;

    extension(CoreDatabaseFixture fixture)
    {
        public async Task<Guid> SaveWorkOrderDraftAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var draft = new WorkOrderDraft(
                Guid.CreateVersion7(),
                companyId,
                machineId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                WorkOrderDraftTestData.Title,
                description: null,
                WorkOrderPriority.High,
                WorkOrderCategory.Repair,
                WorkOrderDraftTestData.ValidEstimate);

            dbContext.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }

        /// <summary>
        /// Stores the proposal as an inbox row of the Insights advisor actor and lets the real inbox processor handle
        /// it, so the handler runs with the actor and the company that the inbox restores.
        /// </summary>
        /// <param name="message">The proposal to receive.</param>
        /// <returns>The state of the inbox row after the run.</returns>
        public async Task<InboxRowOutcome> ReceiveWorkOrderDraftThroughInboxAsync(WorkOrderDraftProposed message)
        {
            var advisor = KnownActors.ForServiceFlow(ServiceName.Insights, flow: "advisor", message.CompanyId);

            return await fixture.ReceiveThroughInboxAsync(
                WorkOrderDraftProposed.MessageType,
                JsonSerializer.Serialize(message, MessagingJsonOptions.Instance),
                advisor.UserId,
                message.CompanyId);
        }

        /// <summary>
        /// Stores a message as an inbox row of the given actor and lets the real inbox processor handle it, so the
        /// handler runs with the actor and the company that the inbox restores, and the runner pushes the events
        /// after the commit.
        /// </summary>
        /// <param name="messageType">The stable contract name of the message.</param>
        /// <param name="payload">The JSON payload of the message.</param>
        /// <param name="actorUserId">The actor ID of the row.</param>
        /// <param name="companyId">The company of the row.</param>
        /// <returns>The state of the inbox row after the run.</returns>
        public async Task<InboxRowOutcome> ReceiveThroughInboxAsync(
            string messageType,
            string payload,
            Guid actorUserId,
            Guid companyId
        )
        {
            var messageId = Guid.CreateVersion7();
            using (var scope = fixture.ScopeFactory.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IInbox>()
                    .TryAddAsync(messageId, messageType, payload, traceParent: null, actorUserId, companyId);
            }

            var processor = fixture.GetSingleton<IEnumerable<IHostedService>>()
                .Single(service => service.GetType().Name == "InboxProcessor");
            await processor.StartAsync(CancellationToken.None);

            try
            {
                for (var wait = 0; wait < InboxWaitSteps; wait++)
                {
                    var outcome = await fixture.LoadInboxRowAsync(messageId);

                    if (outcome.IsProcessed || outcome.AttemptCount > 0)
                    {
                        return outcome;
                    }

                    await Task.Delay(InboxWaitStepMilliseconds);
                }
            }
            finally
            {
                await processor.StopAsync(CancellationToken.None);
            }

            return await fixture.LoadInboxRowAsync(messageId);
        }

        public async Task<InboxRowOutcome> LoadInboxRowAsync(Guid messageId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var row = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(message => message.Id == messageId);

            return new InboxRowOutcome(row.ProcessedOn is not null, row.AttemptCount, row.Error);
        }

        public async Task ProposeWorkOrderDraftAsync(Guid actorCompanyId, WorkOrderDraftProposed message)
        {
            using var scope = fixture.CreateUserScope(actorCompanyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<WorkOrderDraftProposed>>()
                .HandleAsync(message);
        }

        public async Task<ApplyWorkOrderDraftResponse> ApplyWorkOrderDraftAsync(ApplyWorkOrderDraftRequest request)
        {
            using var scope = fixture.CreateUserScope(request.CompanyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<ApplyWorkOrderDraftRequest, ApplyWorkOrderDraftResponse>>()
                .HandleAsync(request);
        }

        public async Task DismissWorkOrderDraftAsync(DismissWorkOrderDraftRequest request)
        {
            using var scope = fixture.CreateUserScope(request.CompanyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<DismissWorkOrderDraftRequest>>()
                .HandleAsync(request);
        }

        public async Task<IReadOnlyList<WorkOrderDraft>> LoadWorkOrderDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .WorkOrderDrafts
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TMessage>> ReadDraftOutboxAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<TMessage>(payload, MessagingJsonOptions.Instance)!)
                .ToList();
        }

        public async Task SetWorkOrderDraftColumnAsync(Guid draftId, string column, string sqlValue)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    format: "UPDATE work_order_drafts SET {0} = {1} WHERE id = {{0}}",
                    column,
                    sqlValue),
                draftId);
        }
    }
}

public sealed record InboxRowOutcome(bool IsProcessed, int AttemptCount, string? Error);
