using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Testing.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Cases.Data;

public static class CaseApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";

    extension(InsightsApiFixture fixture)
    {
        public TestTokenClaims CreateCaseClaims(params string[] permissions) =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendCaseRequestAsync(
            HttpMethod method,
            string path,
            TestTokenClaims claims,
            object? body = null
        )
        {
            using var client = fixture.CreateClient();
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await client.SendAsync(request);
        }

        public async Task<Guid> ArrangeCaseAsync(
            Guid? companyId = null,
            bool isClosed = false,
            SignalSeverity? severity = null
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var machineId = Guid.CreateVersion7();
            var maintenanceCase = new MaintenanceCase(
                companyId ?? fixture.CompanyId,
                machineId,
                CaseApiTestData.Now);
            maintenanceCase.Add(
                new MachineSignal(
                    Guid.CreateVersion7(),
                    machineId,
                    maintenanceCase.CompanyId,
                    new SignalChange
                    {
                        Rule = SignalRule.Threshold,
                        Severity = severity ?? SignalSeverity.Warning,
                        State = SignalState.Open,
                        MetricKey = "spindle.temperature",
                        Value = 91,
                        Limit = 90,
                        OpenedAt = CaseApiTestData.Now,
                        Sequence = 1,
                    }));

            if (isClosed)
            {
                maintenanceCase.Close(reason: "done", CaseApiTestData.Now);
            }

            dbContext.MaintenanceCases.Add(maintenanceCase);
            await dbContext.SaveChangesAsync();

            return maintenanceCase.Id;
        }

        public async Task ArrangeFailedRunAsync(Guid caseId, AgentRunErrorCode errorCode)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var run = new AgentRun(AgentRole.Triage, caseId, ingestAttemptId: null, CaseApiTestData.Now);
            run.Claim(CaseApiTestData.Now, CaseApiTestData.Now.AddMinutes(value: 5));
            run.Fail(CaseApiTestData.Now, errorCode);
            dbContext.AgentRuns.Add(run);

            await dbContext.SaveChangesAsync();
        }

        public async Task ArrangeRunAsync(Guid caseId, AgentRunState state)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var run = new AgentRun(AgentRole.Triage, caseId, ingestAttemptId: null, CaseApiTestData.Now);

            if (state != AgentRunState.Queued)
            {
                run.Claim(CaseApiTestData.Now, CaseApiTestData.Now.AddMinutes(value: 5));
            }

            if (state == AgentRunState.Skipped)
            {
                run.Skip(CaseApiTestData.Now, AgentRunSkipReason.NothingToReview);
            }
            else if (state == AgentRunState.Completed)
            {
                run.Complete(CaseApiTestData.Now, verdict: null, verdictReason: null, inputTokens: 0, outputTokens: 0);
            }

            dbContext.AgentRuns.Add(run);

            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AgentRun>> LoadCaseRunsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .AsNoTracking()
                .OrderBy(run => run.Id)
                .ToListAsync();
        }

        public async Task<MaintenanceCase> LoadCaseAsync(Guid caseId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .MaintenanceCases
                .AsNoTracking()
                .Include(candidate => candidate.Signals)
                .SingleAsync(candidate => candidate.Id == caseId);
        }
    }
}
