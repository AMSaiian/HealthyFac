using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Agents;
using HealthyFac.Insights.Infrastructure.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Agents.Data;

/// <summary>
/// Runs the worker steps of the agents and reads the runs back. Every method opens its own DI scope, so it reads what
/// is stored, not tracked objects.
/// </summary>
public static class AgentWorkerFixtureExtensions
{
    extension(SignalCaseFixture fixture)
    {
        public Task<bool> RunAgentWorkerOnceAsync() =>
            fixture.Inner.GetSingleton<AgentWorker>().RunOnceAsync(CancellationToken.None);

        public async Task<bool> SweepAgentRunsAsync()
        {
            using var scope = fixture.Inner.CreateActorScope(AgentServiceActors.Worker);

            var role = await scope.ServiceProvider.GetRequiredService<AgentRunClaims>()
                .InterruptNextExpiredAsync(CancellationToken.None);

            return role is not null;
        }

        public async Task<ClaimedRun?> ClaimAgentRunAsync()
        {
            using var scope = fixture.Inner.CreateActorScope(AgentServiceActors.Worker);

            return await scope.ServiceProvider.GetRequiredService<AgentRunClaims>()
                .ClaimNextAsync(CancellationToken.None);
        }

        public async Task<Guid> AddRunningRunAsync(Guid caseId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var now = fixture.Time.GetUtcNow().UtcDateTime;
            var run = new AgentRun(AgentRole.Triage, caseId, ingestAttemptId: null, now);

            run.Claim(now, now.AddSeconds(AgentTestData.LeaseSeconds));
            dbContext.AgentRuns.Add(run);
            await dbContext.SaveChangesAsync();

            return run.Id;
        }

        public async Task AddQueuedRunAsync(AgentRole role, Guid caseId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            dbContext.AgentRuns.Add(
                new AgentRun(role, caseId, ingestAttemptId: null, fixture.Time.GetUtcNow().UtcDateTime));
            await dbContext.SaveChangesAsync();
        }

        public async Task<bool> TryStoreRawRunVerdictAsync(Guid runId, string verdict)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            try
            {
                await dbContext.Database.ExecuteSqlAsync(
                    $"UPDATE agent_runs SET verdict = {verdict} WHERE id = {runId}");

                return true;
            }
            catch (Exception exception) when (exception is DbUpdateException or Npgsql.PostgresException)
            {
                return false;
            }
        }

        public async Task<bool> TryStoreRawRunErrorCodeAsync(Guid runId, string errorCode)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            try
            {
                await dbContext.Database.ExecuteSqlAsync(
                    $"UPDATE agent_runs SET error_code = {errorCode} WHERE id = {runId}");

                return true;
            }
            catch (Exception exception) when (exception is DbUpdateException or Npgsql.PostgresException)
            {
                return false;
            }
        }

        public async Task DeleteQueuedRunsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            await dbContext.AgentRuns.Where(run => run.State == AgentRunState.Queued).ExecuteDeleteAsync();
        }

        public async Task AddBufferedActionAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            dbContext.AgentActions.Add(
                new AgentAction(runId, callId: "call-1", AgentActionKind.CloseCase, payload: "{}"));
            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AgentAction>> LoadActionsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentActions
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AgentRun> LoadRunAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .AsNoTracking()
                .SingleAsync(run => run.Id == runId);
        }
    }
}
