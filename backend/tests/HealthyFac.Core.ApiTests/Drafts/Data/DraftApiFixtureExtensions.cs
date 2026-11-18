using System.Text.Json;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Drafts.Data;

public static class DraftApiFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public async Task<ArrangedDraft> ArrangeMachineDraftAsync(
            Guid? targetMachineId = null,
            Guid? companyId = null,
            Guid? attemptId = null
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var owner = companyId ?? fixture.CompanyId;
            var draft = new MachineDraft(
                Guid.CreateVersion7(),
                owner,
                attemptId ?? DraftApiTestData.AttemptId,
                DraftApiTestData.FileId,
                DraftApiTestData.QueuedAt,
                targetMachineId,
                DraftApiTestData.ProposedName,
                DraftApiTestData.ProposedSerialNumber,
                description: null);
            dbContext.MachineDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return new ArrangedDraft(draft.Id, fixture.IdEncryptor.Encrypt(draft.Id));
        }

        public async Task<uint> LoadMachineRowVersionAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .Where(candidate => candidate.Id == machineId)
                .Select(candidate => candidate.RowVersion)
                .SingleAsync();
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

        public async Task<(string Name, string SerialNumber)> LoadMachineValuesAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var machine = await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == machineId);

            return (machine.Name, machine.SerialNumber);
        }

        public async Task<IReadOnlyList<TMessage>> ReadOutboxMessagesAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<TMessage>(payload, JsonSerializerOptions.Web)!)
                .ToList();
        }
    }
}

/// <summary>
/// A draft that a test stored.
/// </summary>
/// <param name="Id">The ID of the draft.</param>
/// <param name="EncryptedId">The encrypted ID for the route.</param>
public sealed record ArrangedDraft(Guid Id, string EncryptedId);
