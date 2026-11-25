using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Infrastructure.Database;
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
        public async Task<IReadOnlyList<DraftProposal>> LoadDraftProposalsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .DraftProposals
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SaveDraftProposalAsync(DraftProposal proposal)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.DraftProposals.Add(proposal);
            await dbContext.SaveChangesAsync();
        }

        public async Task SetDraftProposalColumnAsync(Guid proposalId, string column, string sqlValue)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    format: "UPDATE draft_proposals SET {0} = {1} WHERE id = {{0}}",
                    column,
                    sqlValue),
                proposalId);
        }

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
                .Include(draft => draft.Proposal)
                .ToListAsync();
        }

        public async Task AddDraftsForMachineAsync(Guid companyId, Guid machineId, Guid? attemptId = null)
        {
            var attempt = attemptId ?? DraftTestData.AttemptId;
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            await EnsureProposalAsync(dbContext, companyId, attempt);
            dbContext.MachineDrafts.Add(
                new MachineDraft(
                    Guid.CreateVersion7(),
                    companyId,
                    attempt,
                    machineId,
                    DraftTestData.MachineName,
                    serialNumber: null,
                    description: null));

            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> AddChatMachineDraftAsync(
            Guid companyId,
            Guid? targetMachineId = null,
            Guid? chatId = null,
            Guid? chatMessageId = null
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var proposal = DraftProposal.ForChat(
                chatMessageId ?? DraftTestData.ChatMessageId,
                companyId,
                chatId ?? DraftTestData.ChatId);
            dbContext.DraftProposals.Add(proposal);
            var draft = new MachineDraft(
                Guid.CreateVersion7(),
                companyId,
                proposal.Id,
                targetMachineId,
                DraftTestData.MachineName,
                serialNumber: null,
                description: null);

            dbContext.MachineDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }

        public async Task<Guid> AddMachineDraftAsync(Guid companyId, Guid? targetMachineId = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            await EnsureProposalAsync(dbContext, companyId, DraftTestData.AttemptId);
            var draft = new MachineDraft(
                Guid.CreateVersion7(),
                companyId,
                DraftTestData.AttemptId,
                targetMachineId,
                DraftTestData.MachineName,
                serialNumber: null,
                description: null);

            dbContext.MachineDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }
    }

    private static async Task EnsureProposalAsync(ICoreDbContext dbContext, Guid companyId, Guid attemptId)
    {
        var isStored = await dbContext.DraftProposals.AnyAsync(proposal => proposal.Id == attemptId);

        if (!isStored)
        {
            dbContext.DraftProposals.Add(
                DraftProposal.ForIngest(attemptId, companyId, DraftTestData.FileId, DraftTestData.QueuedAt));
        }
    }
}
