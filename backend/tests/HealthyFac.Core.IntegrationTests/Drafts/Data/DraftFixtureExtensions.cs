using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<Guid> ArrangeMachineAsync(Guid companyId)
            => await fixture.CreateMachineAsync(
                companyId, DraftTestData.MachineName, Guid.NewGuid().ToString(format: "N"));

        public async Task StoreProposedDraftsAsync(Guid actorCompanyId, DraftsProposed message)
        {
            using var scope = fixture.CreateUserScope(actorCompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DraftsProposed>>();

            await handler.HandleAsync(message);
        }

        public async Task<IReadOnlyList<MachineDraft>> LoadMachineDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .MachineDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddDraftsForMachineAsync(Guid companyId, Guid machineId, Guid? attemptId = null)
        {
            var attempt = attemptId ?? DraftTestData.AttemptId;
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.MachineDrafts.Add(
                new MachineDraft(
                    Guid.CreateVersion7(),
                    companyId,
                    attempt,
                    DraftTestData.FileId,
                    DraftTestData.QueuedAt,
                    machineId,
                    DraftTestData.MachineName,
                    serialNumber: null,
                    description: null));
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> AddMachineDraftAsync(Guid companyId, Guid? targetMachineId = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var draft = new MachineDraft(
                Guid.CreateVersion7(),
                companyId,
                DraftTestData.AttemptId,
                DraftTestData.FileId,
                DraftTestData.QueuedAt,
                targetMachineId,
                DraftTestData.MachineName,
                serialNumber: null,
                description: null);

            dbContext.MachineDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }
    }
}
