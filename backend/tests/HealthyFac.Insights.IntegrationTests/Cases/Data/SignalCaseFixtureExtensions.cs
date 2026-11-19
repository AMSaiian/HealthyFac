using System.Text.Json;
using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Telemetry.Messaging.Signals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Insights.IntegrationTests.Cases.Data;

/// <summary>
/// Sends signal messages to the handler and reads the cases, the signals, and the runs back. Every method opens its
/// own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class SignalCaseFixtureExtensions
{
    private const string InboxProcessorTypeName = "InboxProcessor";

    extension(SignalCaseFixture fixture)
    {
        public async Task HandleSignalAsync(SignalEpisodeChanged message)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<SignalEpisodeChanged>>();

            await handler.HandleAsync(message);
        }

        /// <summary>
        /// Stores the message as an inbox row and lets the real inbox processor handle it, so the handler runs in
        /// the transaction of the inbox runner. The method waits until the row is processed or failed.
        /// </summary>
        /// <param name="message">The message to receive.</param>
        /// <returns>The state of the inbox row after the run.</returns>
        public Task<InboxOutcome> ReceiveSignalThroughInboxAsync(SignalEpisodeChanged message) =>
            fixture.ReceiveThroughInboxAsync(message, message.CompanyId);

        /// <summary>
        /// Stores any integration message as an inbox row and lets the real inbox processor handle it. The method waits
        /// until the row is processed or failed.
        /// </summary>
        /// <param name="message">The message to receive.</param>
        /// <param name="companyId">The company that the inbox row restores for the handler.</param>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <returns>The state of the inbox row after the run.</returns>
        public async Task<InboxOutcome> ReceiveThroughInboxAsync<TMessage>(TMessage message, Guid companyId)
            where TMessage : IIntegrationMessage
        {
            var messageId = Guid.CreateVersion7();
            using (var scope = fixture.Inner.ScopeFactory.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IInbox>()
                    .TryAddAsync(
                        messageId,
                        TMessage.MessageType,
                        JsonSerializer.Serialize(message, MessagingJsonOptions.Instance),
                        traceParent: null,
                        Guid.CreateVersion7(),
                        companyId);
            }

            var processor = fixture.Inner.GetSingleton<IEnumerable<IHostedService>>()
                .Single(service => service.GetType().Name == InboxProcessorTypeName);
            await processor.StartAsync(CancellationToken.None);

            try
            {
                for (var wait = 0; wait < CaseTestData.InboxWaitSteps; wait++)
                {
                    var outcome = await fixture.LoadInboxOutcomeAsync(messageId);

                    if (outcome.IsProcessed || outcome.AttemptCount > 0)
                    {
                        return outcome;
                    }

                    await Task.Delay(CaseTestData.InboxWaitStepMilliseconds);
                }
            }
            finally
            {
                await processor.StopAsync(CancellationToken.None);
            }

            return await fixture.LoadInboxOutcomeAsync(messageId);
        }

        public async Task<InboxOutcome> LoadInboxOutcomeAsync(Guid messageId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var row = await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(message => message.Id == messageId);

            return new InboxOutcome(row.ProcessedOn is not null, row.AttemptCount, row.Error);
        }

        public async Task<List<MachineSignal>> LoadSignalsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .MachineSignals
                .AsNoTracking()
                .OrderBy(signal => signal.OpenedAt)
                .ThenBy(signal => signal.Id)
                .ToListAsync();
        }

        public async Task<MachineSignal> LoadSignalAsync(Guid episodeId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .MachineSignals
                .AsNoTracking()
                .SingleAsync(signal => signal.Id == episodeId);
        }

        public async Task<List<MaintenanceCase>> LoadCasesAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .MaintenanceCases
                .AsNoTracking()
                .OrderBy(maintenanceCase => maintenanceCase.OpenedAt)
                .ThenBy(maintenanceCase => maintenanceCase.Id)
                .ToListAsync();
        }

        public async Task<List<AgentRun>> LoadRunsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .AsNoTracking()
                .OrderBy(run => run.Id)
                .ToListAsync();
        }

        public async Task ClearRunsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            await dbContext.AgentRuns.ExecuteDeleteAsync();
        }

        public async Task DismissSignalAsync(Guid episodeId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var signal = await dbContext.MachineSignals.SingleAsync(candidate => candidate.Id == episodeId);

            signal.Dismiss();
            await dbContext.SaveChangesAsync();
        }

        public async Task DraftSignalAsync(Guid episodeId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var signal = await dbContext.MachineSignals.SingleAsync(candidate => candidate.Id == episodeId);

            signal.MarkDrafted(Guid.CreateVersion7());
            await dbContext.SaveChangesAsync();
        }

        public async Task DraftSignalWithDraftIdAsync(Guid episodeId, Guid draftId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var signal = await dbContext.MachineSignals.SingleAsync(candidate => candidate.Id == episodeId);

            signal.MarkDrafted(draftId);
            await dbContext.SaveChangesAsync();
        }

        public async Task HandleWorkOrderDraftAppliedAsync(WorkOrderDraftApplied message)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<WorkOrderDraftApplied>>()
                .HandleAsync(message);
        }

        public async Task CloseCaseAsync(Guid caseId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var maintenanceCase = await dbContext.MaintenanceCases
                .Include(candidate => candidate.Signals)
                .SingleAsync(candidate => candidate.Id == caseId);

            maintenanceCase.Close(reason: "closed by test", CaseTestData.Now);
            await dbContext.SaveChangesAsync();
        }
    }
}

public sealed record InboxOutcome(bool IsProcessed, int AttemptCount, string? Error);
