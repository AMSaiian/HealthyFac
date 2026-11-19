using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Extraction.Data;

public static class ExtractionFixtureExtensions
{
    extension(InsightsFixture fixture)
    {
        public async Task<IReadOnlyList<AgentRun>> LoadExtractorRunsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .AsNoTracking()
                .Where(run => run.Role == AgentRole.Extractor)
                .OrderBy(run => run.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<AgentToolCall>> LoadExtractorToolCallsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentToolCalls
                .AsNoTracking()
                .OrderBy(call => call.RunId)
                .ThenBy(call => call.Sequence)
                .ToListAsync();
        }
    }
}
