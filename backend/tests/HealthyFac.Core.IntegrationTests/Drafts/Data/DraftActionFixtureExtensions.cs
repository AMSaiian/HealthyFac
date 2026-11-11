using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftActionFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task ApplyDraftAsync(Guid companyId, Guid draftId, string name, string serialNumber)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<ApplyMachineDraftRequest, ApplyMachineDraftResponse>>();

            await handler.HandleAsync(
                new ApplyMachineDraftRequest
                {
                    CompanyId = companyId,
                    Id = draftId,
                    Name = name,
                    SerialNumber = serialNumber,
                });
        }

        public async Task<int> CountMachinesAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.Machines.CountAsync(machine => machine.CompanyId == companyId);
        }

        public async Task<DismissMachineDraftsResponse> DismissMachineDraftsAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DismissMachineDraftsRequest, DismissMachineDraftsResponse>>();

            return await handler.HandleAsync(
                new DismissMachineDraftsRequest { CompanyId = companyId, MachineId = machineId });
        }

        public async Task MarkDraftsAppliedAsync(Guid companyId, params Guid[] ids)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MarkDraftsAppliedRequest>>();

            await handler.HandleAsync(new MarkDraftsAppliedRequest { CompanyId = companyId, Ids = ids });
        }

        public async Task DismissDraftAsync(Guid companyId, Guid draftId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DismissDraftRequest>>();

            await handler.HandleAsync(new DismissDraftRequest { CompanyId = companyId, Id = draftId });
        }
    }
}
