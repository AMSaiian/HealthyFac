using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Cases.Data;

public static class CasesFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<(MaintenanceCase Case, MachineSignal Signal)> LoadOpenCaseAsync()
        {
            using var scope = fixture.Hosts.Insights.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            var maintenanceCase = await dbContext.MaintenanceCases
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == CasesTestData.CaseId);

            var signal = await dbContext.MachineSignals
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == CasesTestData.SignalId);

            return (maintenanceCase, signal);
        }

        public async Task<WorkOrderDraft> LoadWorkOrderDraftAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .WorkOrderDrafts
                .AsNoTracking()
                .SingleAsync(draft => draft.Id == CasesTestData.DraftId);
        }
    }
}
