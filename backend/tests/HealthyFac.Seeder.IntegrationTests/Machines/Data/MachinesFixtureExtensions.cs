using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Machines.Data;

public static class MachinesFixtureExtensions
{
    private const string ApplyItem = "apply-set-2";

    extension(SeederFixture fixture)
    {
        public async Task<List<Machine>> LoadMachinesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Machines
                .Include(machine => machine.StateHistory)
                .AsNoTracking()
                .OrderBy(machine => machine.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MachineDraft>> LoadMachineDraftsAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .MachineDrafts
                .AsNoTracking()
                .Include(draft => draft.Proposal)
                .OrderBy(draft => draft.Proposal.QueuedAt)
                .ToListAsync();
        }

        public async Task<TelemetryDefinitionCounts> CountTelemetryDefinitionsAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
            var registrationCount = await dbContext.MachineRegistrations.CountAsync();
            var metricCount = await dbContext.MetricDefinitions.CountAsync();
            var coolantCount = await dbContext.MetricDefinitions.CountAsync(m => m.Key == MachinesTestData.CoolantKey);
            var statusCount = await dbContext.StatusDefinitions.CountAsync();
            var phaseCount = await dbContext.OperatingPhases.CountAsync();

            return new TelemetryDefinitionCounts(registrationCount, metricCount, coolantCount, statusCount, phaseCount);
        }

        public async Task<bool> IsCoolantDraftPendingWithoutKindAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .MetricDefinitionDrafts
                .AnyAsync(draft => draft.Key == MachinesTestData.CoolantKey
                    && draft.State == DefinitionDraftState.Pending
                    && draft.ValueKind == null);
        }

        public async Task<List<FileRef>> LoadTiedFilesAsync()
        {
            var files = await fixture.LoadFileRefsAsync();

            return [.. files.OrderBy(file => file.CreatedAt)];
        }

        public async Task ApplyPendingMachineDraftsAsync(SeedContext context, CancellationToken cancellationToken)
        {
            var allDrafts = await fixture.LoadMachineDraftsAsync();
            fixture.Clock.Enter(PeopleTestData.Anchor, ApplyItem);

            foreach (var draft in allDrafts.Where(draft => draft.State == DraftState.Pending))
            {
                await fixture.Hosts.Core.SendAsync<ApplyMachineDraftRequest, ApplyMachineDraftResponse>(
                    context.GetActor(person: "nadiia"),
                    new ApplyMachineDraftRequest
                    {
                        CompanyId = PeopleTestData.CompanyId,
                        Id = draft.Id,
                        Name = draft.Name!,
                        SerialNumber = draft.SerialNumber!,
                        Description = draft.Description,
                    },
                    cancellationToken);
            }

            await fixture.Hosts.CreateRelay().RunAsync(ApplyItem, cancellationToken);
        }
    }
}

public sealed record TelemetryDefinitionCounts(
    int Registrations,
    int Metrics,
    int Coolants,
    int Statuses,
    int Phases
);
