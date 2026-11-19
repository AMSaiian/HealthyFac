using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// Stores and reads definition drafts and proposal entries. Every method opens its own DI scope, so it reads what is
/// stored, not tracked objects.
/// </summary>
public static class DefinitionDraftFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<StoredDraftRows> StoreDraftRowsAsync(Guid? tentativeMachineId = null)
        {
            var entry = new ProposalEntry(Guid.CreateVersion7(), DefinitionDraftTestData.CompanyId);
            var metric = DefinitionDraftTestData.CreateMetric(Guid.CreateVersion7(), entry.Id, tentativeMachineId);
            var status = DefinitionDraftTestData.CreateStatus(Guid.CreateVersion7(), entry.Id, tentativeMachineId);

            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            dbContext.ProposalEntries.Add(entry);
            dbContext.MetricDefinitionDrafts.Add(metric);
            dbContext.StatusDefinitionDrafts.Add(status);
            await dbContext.SaveChangesAsync();

            return new StoredDraftRows { MetricId = metric.Id, StatusId = status.Id, EntryId = entry.Id };
        }

        public async Task ExecuteSqlAsync(string sql, Guid rowId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync(sql, rowId);
        }

        public async Task StoreProposedDefinitionDraftsAsync(Guid actorCompanyId, DraftsProposed message)
        {
            using var scope = fixture.CreateUserScope(actorCompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DraftsProposed>>();

            await handler.HandleAsync(message);
        }

        public async Task ResolveDraftsAsync(DraftsResolved message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DraftsResolved>>();

            await handler.HandleAsync(message);
        }

        public async Task ApplyMachineDraftMessageAsync(MachineDraftApplied message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDraftApplied>>();

            await handler.HandleAsync(message);
        }

        public async Task DismissMachineDraftMessageAsync(MachineDraftDismissed message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDraftDismissed>>();

            await handler.HandleAsync(message);
        }

        public async Task DismissMetricDraftAsync(Guid draftId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var draft = await dbContext.MetricDefinitionDrafts.SingleAsync(stored => stored.Id == draftId);

            draft.Dismiss();
            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<MetricDefinitionDraft>> LoadMetricDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<OperatingPhaseDraft>> LoadPhaseDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .OperatingPhaseDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Include(draft => draft.Conditions)
                .Include(draft => draft.Limits)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<StatusDefinitionDraft>> LoadStatusDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .StatusDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ProposalEntry>> LoadProposalEntriesAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .ProposalEntries
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SoftDeleteMetricDraftAsync(Guid draftId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var draft = await dbContext.MetricDefinitionDrafts.SingleAsync(stored => stored.Id == draftId);

            dbContext.MetricDefinitionDrafts.Remove(draft);
            await dbContext.SaveChangesAsync();
        }

        public async Task<KeysetGridResponse<DefinitionDraftGridItem>> SearchDefinitionDraftsAsync(
            Guid companyId,
            Guid? machineId,
            Guid? attemptId,
            int limit,
            GridSort<DefinitionDraftSortColumn>? sort = null,
            KeysetPosition? after = null,
            DefinitionDraftGridFilter? filter = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<SearchDefinitionDraftsRequest, KeysetGridResponse<DefinitionDraftGridItem>>>();

            return await handler.HandleAsync(
                new SearchDefinitionDraftsRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    AttemptId = attemptId,
                    Limit = limit,
                    Sort = sort,
                    Filter = filter ?? new DefinitionDraftGridFilter(),
                    After = after,
                });
        }

        public async Task<IReadOnlyList<Guid>> ReadAllDefinitionDraftPagesAsync(
            Guid companyId,
            Guid machineId,
            GridSort<DefinitionDraftSortColumn> sort
        )
        {
            var seenIds = new List<Guid>();
            KeysetPosition? after = null;

            do
            {
                var page = await fixture.SearchDefinitionDraftsAsync(
                    companyId, machineId, attemptId: null, DefinitionDraftSearchTestData.PageLimit, sort, after);
                seenIds.AddRange(page.Items.Select(item => item.Id));
                after = page.NextPosition;
            }
            while (after is not null && seenIds.Count <= DefinitionDraftSearchTestData.DraftCount);

            return seenIds;
        }

        public async Task<AttemptDefinitionDraftsResponse> GetAttemptDefinitionDraftsAsync(
            Guid companyId,
            Guid attemptId
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<GetAttemptDefinitionDraftsRequest, AttemptDefinitionDraftsResponse>>();

            return await handler.HandleAsync(
                new GetAttemptDefinitionDraftsRequest { CompanyId = companyId, AttemptId = attemptId });
        }

        public async Task<PendingDefinitionDraftCountsResponse> GetPendingDefinitionDraftCountsAsync(Guid companyId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<GetPendingDefinitionDraftCountsRequest, PendingDefinitionDraftCountsResponse>>();

            return await handler.HandleAsync(new GetPendingDefinitionDraftCountsRequest { CompanyId = companyId });
        }

        public async Task DismissDefinitionDraftAsync(Guid companyId, Guid draftId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DismissDefinitionDraftRequest>>();

            await handler.HandleAsync(new DismissDefinitionDraftRequest { CompanyId = companyId, Id = draftId });
        }

        public async Task<DismissDefinitionDraftsResponse> DismissDefinitionDraftsAsync(
            Guid companyId,
            Guid? machineId,
            Guid? attemptId
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<DismissDefinitionDraftsRequest, DismissDefinitionDraftsResponse>>();

            return await handler.HandleAsync(
                new DismissDefinitionDraftsRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    AttemptId = attemptId,
                });
        }

        public async Task<MetricDefinitionDraft> LoadMetricDraftAsync(Guid draftId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricDefinitionDrafts
                .AsNoTracking()
                .SingleAsync(draft => draft.Id == draftId);
        }
    }
}
