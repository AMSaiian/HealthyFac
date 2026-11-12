using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

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

        public async Task<IReadOnlyList<MetricDefinitionDraft>> LoadMetricDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .MetricDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<StatusDefinitionDraft>> LoadStatusDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .StatusDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddDraftsForMachineAsync(Guid companyId, Guid machineId, Guid? attemptId = null)
        {
            var attempt = attemptId ?? DraftTestData.AttemptId;
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var applied = MetricDefinitionDraft.ForMachine(
                Guid.CreateVersion7(),
                companyId,
                attempt,
                DraftTestData.FileId,
                machineId,
                new MetricDraftValues { Key = DraftTestData.AppliedKey });
            applied.MarkApplied();

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
            dbContext.MetricDefinitionDrafts.Add(
                MetricDefinitionDraft.ForMachine(
                    Guid.CreateVersion7(),
                    companyId,
                    attempt,
                    DraftTestData.FileId,
                    machineId,
                    new MetricDraftValues { Key = DraftTestData.PendingKey }));
            dbContext.MetricDefinitionDrafts.Add(applied);
            dbContext.StatusDefinitionDrafts.Add(
                StatusDefinitionDraft.ForMachine(
                    Guid.CreateVersion7(),
                    companyId,
                    attempt,
                    DraftTestData.FileId,
                    machineId,
                    new StatusDraftValues { Code = "E1" }));
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> AddMachineDraftAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var draft = new MachineDraft(
                Guid.CreateVersion7(),
                companyId,
                DraftTestData.AttemptId,
                DraftTestData.FileId,
                DraftTestData.QueuedAt,
                targetMachineId: null,
                DraftTestData.MachineName,
                serialNumber: null,
                description: null);

            dbContext.MachineDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }

        /// <summary>
        /// Inserts a child draft row with raw SQL, so the row can hold a target combination that the entity refuses.
        /// </summary>
        /// <param name="table">The table of the child draft.</param>
        /// <param name="companyId">The company of the row.</param>
        /// <param name="targetMachineId">The machine target, or <see langword="null"/>.</param>
        /// <param name="targetMachineDraftId">The machine draft target, or <see langword="null"/>.</param>
        /// <returns>A task that completes when the insert ends.</returns>
        public async Task InsertChildDraftRowAsync(
            string table,
            Guid companyId,
            Guid? targetMachineId,
            Guid? targetMachineDraftId
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var sql = $"INSERT INTO {table} (id, company_id, attempt_id, file_id, target_machine_draft_id, "
                + "target_machine_id, state, created_at, created_by) "
                + "VALUES (@id, @company, @attempt, @file, @draft, @machine, 'pending', now(), @actor)";

            await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                Parameter(name: "id", Guid.CreateVersion7()),
                Parameter(name: "company", companyId),
                Parameter(name: "attempt", DraftTestData.AttemptId),
                Parameter(name: "file", DraftTestData.FileId),
                Parameter(name: "draft", targetMachineDraftId),
                Parameter(name: "machine", targetMachineId),
                Parameter(name: "actor", Guid.CreateVersion7()));
        }
    }

    private static NpgsqlParameter Parameter(string name, Guid? value) =>
        new(name, NpgsqlDbType.Uuid) { Value = (object?)value ?? DBNull.Value };
}
