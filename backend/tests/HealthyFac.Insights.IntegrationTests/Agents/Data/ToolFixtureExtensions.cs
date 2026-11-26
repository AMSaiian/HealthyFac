using System.Text.Json;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Agents.Data;

/// <summary>
/// Calls an agent tool the way a run does: through the registry, with the context of a case, and reads the logged
/// calls and the seeded manual chunks back.
/// </summary>
public static class ToolFixtureExtensions
{
    extension(SignalCaseFixture fixture)
    {
        /// <summary>
        /// Calls the planner tool inside a scope that records the call for a new running run of the first case.
        /// </summary>
        public async Task<string> CallToolAsync(
            string toolName,
            string argumentsJson = CaseTestData.EmptyJson,
            Guid? machineId = null,
            Guid? existingRunId = null,
            string? callId = null,
            ManualPassageLog? manualPassages = null
        )
        {
            var caseId = (await fixture.LoadCasesAsync())[0].Id;
            var runId = existingRunId ?? await fixture.AddRunningRunAsync(caseId);
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var run = await dbContext.AgentRuns.SingleAsync(candidate => candidate.Id == runId);
            var lease = scope.ServiceProvider.GetRequiredService<IAgentRunLease>();

            var context = new ToolContext
            {
                CompanyId = CaseTestData.CompanyId,
                MachineId = machineId ?? CaseTestData.MachineId,
                RunId = runId,
                Recorder = ToolCallRecorder.ForDatabase(dbContext, runId),
                CaseId = caseId,
                ManualPassages = manualPassages ?? new ManualPassageLog(),
                CurrentCallId = callId is null ? () => Guid.NewGuid().ToString(format: "N") : () => callId,
                RenewLease = token => lease.RenewAsync(run, token),
            };

            var function = fixture.Inner.GetSingleton<ToolRegistry>()
                .For(AgentRole.Planner, context)
                .Single(candidate => candidate.Name == toolName);

            var arguments = new AIFunctionArguments(
                JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson));

            var result = await function.InvokeAsync(arguments);

            return result!.ToString()!;
        }

        public async Task CommitRunAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var run = await dbContext.AgentRuns.SingleAsync(candidate => candidate.Id == runId);

            var maintenanceCase = await dbContext.MaintenanceCases
                .Include(candidate => candidate.Signals)
                .SingleAsync(candidate => candidate.Id == run.CaseId);

            await scope.ServiceProvider.GetRequiredService<RunCommit>()
                .CommitAsync(
                    run,
                    maintenanceCase,
                    verdict: null,
                    verdictReason: null,
                    inputTokens: 0,
                    outputTokens: 0,
                    CancellationToken.None);
        }

        public async Task FailRunAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            await scope.ServiceProvider.GetRequiredService<RunCommit>()
                .FailAsync(runId, AgentRunErrorCode.UnexpectedError, CancellationToken.None);
        }

        public async Task<IReadOnlyList<TMessage>> ReadOutboxAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;

            var payloads = await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return
            [
                .. payloads.Select(payload =>
                    JsonSerializer.Deserialize<TMessage>(payload, MessagingJsonOptions.Instance)!),
            ];
        }

        public async Task<IReadOnlyList<AgentToolCall>> LoadToolCallsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentToolCalls
                .AsNoTracking()
                .OrderBy(call => call.Sequence)
                .ToListAsync();
        }

        public async Task SeedManualAsync(Guid companyId, Guid machineId, params string[] chunkTexts)
        {
            var attempt = new IngestAttempt(
                companyId,
                Guid.CreateVersion7(),
                machineId,
                CaseTestData.Now,
                fileName: "manual",
                extension: "pdf",
                sizeBytes: 1024,
                CaseTestData.Now,
                traceParent: null);

            var document = new IngestedDocument(
                attempt.FileId,
                companyId,
                machineId,
                CaseTestData.Now,
                attempt.Id,
                extension: "pdf",
                pageCount: 1);

            foreach (var text in chunkTexts)
            {
                document.AddChunk(
                    $"page {document.Chunks.Count + 1}",
                    text,
                    [.. Enumerable.Repeat(element: 1f, IngestAttemptTestData.EmbeddingDimensions)]);
            }

            await fixture.Inner.SaveAsync(attempt, document);
        }
    }
}
