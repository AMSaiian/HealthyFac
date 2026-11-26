using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// Stores and reads definition drafts and proposal entries. Every method opens its own DI scope, so it reads what is
/// stored, not tracked objects.
/// </summary>
public static class DefinitionDraftFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<IReadOnlyList<DraftProposal>> LoadDraftProposalsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .DraftProposals
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SaveDraftProposalAsync(DraftProposal proposal)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            await dbContext.EnsureProposalAsync(proposal);
            await dbContext.SaveChangesAsync();
        }

        public async Task SetDraftProposalColumnAsync(Guid proposalId, string column, string sqlValue)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    format: "UPDATE draft_proposals SET {0} = {1} WHERE id = {{0}}",
                    column,
                    sqlValue),
                proposalId);
        }

        public async Task<StoredDraftRows> StoreDraftRowsAsync(Guid? tentativeMachineId = null)
        {
            var entry = new ProposalEntry(Guid.CreateVersion7(), DefinitionDraftTestData.CompanyId);
            var proposal = DefinitionDraftTestData.CreateProposal();

            var metric = DefinitionDraftTestData.CreateMetric(
                Guid.CreateVersion7(), entry.Id, proposal.Id, tentativeMachineId);

            var status = DefinitionDraftTestData.CreateStatus(
                Guid.CreateVersion7(), entry.Id, proposal.Id, tentativeMachineId);

            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            await dbContext.EnsureProposalAsync(proposal);
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

        public void ClearLogs()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            scope.ServiceProvider.GetFakeLogCollector().Clear();
        }

        public IReadOnlyList<FakeLogRecord> ReadWarnings<THandler>()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var category = typeof(THandler).FullName;

            return
            [
                .. scope.ServiceProvider.GetFakeLogCollector()
                    .GetSnapshot()
                    .Where(record => record.Level == LogLevel.Warning && record.Category == category),
            ];
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
                .Include(draft => draft.Proposal)
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

        public async Task<AttemptDefinitionDraftsResponse> GetChatDefinitionDraftsAsync(Guid companyId, Guid chatId)
        {
            using var scope = fixture.CreateUserScope(companyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<GetAttemptDefinitionDraftsRequest, AttemptDefinitionDraftsResponse>>();

            return await handler.HandleAsync(
                new GetAttemptDefinitionDraftsRequest { CompanyId = companyId, ChatId = chatId });
        }

        public async Task<IReadOnlyList<Guid>> ReadChatDefinitionDraftsAsync(Guid companyId, Guid chatId)
        {
            using var scope = fixture.CreateUserScope(companyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<SearchDefinitionDraftsRequest, KeysetGridResponse<DefinitionDraftGridItem>>>();

            var page = await handler.HandleAsync(
                new SearchDefinitionDraftsRequest
                {
                    CompanyId = companyId,
                    ChatId = chatId,
                    Limit = DefinitionDraftSearchTestData.ChatPageLimit,
                    Filter = new DefinitionDraftGridFilter(),
                });

            return [.. page.Items.Select(item => item.Id)];
        }

        public async Task<DismissDefinitionDraftsResponse> DismissChatDefinitionDraftsAsync(
            Guid companyId,
            Guid chatId
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<DismissDefinitionDraftsRequest, DismissDefinitionDraftsResponse>>();

            return await handler.HandleAsync(
                new DismissDefinitionDraftsRequest { CompanyId = companyId, ChatId = chatId });
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

    public static async Task EnsureProposalAsync(this ITelemetryDbContext dbContext, DraftProposal proposal)
    {
        var isStored = await dbContext.DraftProposals.AnyAsync(candidate => candidate.Id == proposal.Id);

        if (!isStored)
        {
            dbContext.DraftProposals.Add(proposal);
        }
    }
}
