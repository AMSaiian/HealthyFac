using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

public static class ScreeningFixtureExtensions
{
    extension(InsightsFixture fixture)
    {
        public async Task<IReadOnlyList<AgentRun>> LoadScreenerRunsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .AsNoTracking()
                .Where(run => run.Role == AgentRole.Screener)
                .OrderBy(run => run.Id)
                .ToListAsync();
        }

        public void ArrangeChunks(int count)
        {
            var chunks = Enumerable.Range(start: 1, count)
                .Select(number => new ConvertedChunk { Location = $"page {number}", Text = $"Text of chunk {number}." })
                .ToList();
            fixture.Ingest.Converter.Behavior = _ => Task.FromResult(
                new ConvertedDocument { PageCount = count, Chunks = chunks });
        }
    }
}
