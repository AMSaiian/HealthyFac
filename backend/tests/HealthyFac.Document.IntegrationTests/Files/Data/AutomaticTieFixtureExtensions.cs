using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Document.Application;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class AutomaticTieFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task DeliverAutomaticTieAsync(
            AutomaticTieSource source,
            Guid fileId,
            Guid companyId,
            Guid machineId,
            DateTime queuedAt
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            if (source == AutomaticTieSource.DraftApplied)
            {
                await scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDraftApplied>>()
                    .HandleAsync(
                        new MachineDraftApplied
                        {
                            DraftId = Guid.CreateVersion7(),
                            FileId = fileId,
                            MachineId = machineId,
                            CompanyId = companyId,
                            QueuedAt = queuedAt,
                        });

                return;
            }

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<FileMachineMatched>>()
                .HandleAsync(
                    new FileMachineMatched
                    {
                        FileId = fileId,
                        MachineId = machineId,
                        CompanyId = companyId,
                        QueuedAt = queuedAt,
                    });
        }

        public async Task ApplyIngestStateAsync(IngestStateChanged message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<IngestStateChanged>>()
                .HandleAsync(message);
        }

        public async Task TieDirectlyAsync(Guid fileId, Guid machineId, DateTime changedAt)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var file = await dbContext.FileRefs.SingleAsync(candidate => candidate.Id == fileId);

            file.TieTo(machineId, changedAt);
            await dbContext.SaveChangesAsync();
        }

        public async Task UntieDirectlyAsync(Guid fileId, DateTime changedAt)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var file = await dbContext.FileRefs.SingleAsync(candidate => candidate.Id == fileId);

            file.Untie(changedAt);
            await dbContext.SaveChangesAsync();
        }
    }
}

public enum AutomaticTieSource
{
    DraftApplied,
    MachineMatched,
}
