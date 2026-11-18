using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftActionFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task ApplyDraftAsync(
            Guid companyId,
            Guid draftId,
            string name,
            string serialNumber,
            DateOnly? expectedGoLiveOn = null,
            uint? rowVersion = null
        )
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
                    ExpectedGoLiveOn = expectedGoLiveOn,
                    RowVersion = rowVersion,
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

        public async Task<DismissMachineDraftsResponse> DismissAttemptDraftsAsync(Guid companyId, Guid attemptId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DismissMachineDraftsRequest, DismissMachineDraftsResponse>>();

            return await handler.HandleAsync(
                new DismissMachineDraftsRequest { CompanyId = companyId, AttemptId = attemptId });
        }

        public async Task<IReadOnlyList<Guid>> ReadAllDraftPagesAsync(
            Guid companyId,
            Guid machineId,
            GridSort<MachineDraftSortColumn> sort
        )
        {
            var seenIds = new List<Guid>();
            KeysetPosition? after = null;

            do
            {
                using var scope = fixture.CreateUserScope(companyId);
                var handler = scope.ServiceProvider
                    .GetRequiredService<
                        IRequestHandler<SearchMachineDraftsRequest, KeysetGridResponse<MachineDraftGridItem>>>();
                var page = await handler.HandleAsync(
                    new SearchMachineDraftsRequest
                    {
                        CompanyId = companyId,
                        MachineId = machineId,
                        Limit = DraftSearchTestData.PageLimit,
                        Sort = sort,
                        Filter = new MachineDraftGridFilter(),
                        After = after,
                    });
                seenIds.AddRange(page.Items.Select(item => item.Id));
                after = page.NextPosition;
            }
            while (after is not null && seenIds.Count <= DraftSearchTestData.DraftCount);

            return seenIds;
        }

        public async Task DismissDraftAsync(Guid companyId, Guid draftId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DismissDraftRequest>>();

            await handler.HandleAsync(new DismissDraftRequest { CompanyId = companyId, Id = draftId });
        }
    }
}
