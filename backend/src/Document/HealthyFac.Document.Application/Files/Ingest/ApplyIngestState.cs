using HealthyFac.Document.Domain.Files;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Ingest;

public sealed class ApplyIngestStatePreHandlerCheck : PassThroughPreHandlerCheck<IngestStateChanged>;

/// <summary>
/// Stores the ingest state that Insights reported, so the company file grid shows it without a call to Insights. A
/// file that does not exist or is deleted completes without a change. A state that is not newer than the stored one
/// changes nothing (see <see cref="FileRef.ApplyIngestState"/>).
/// </summary>
/// <param name="dbContext">Loads the file ref and saves the change.</param>
public sealed class ApplyIngestStateRequestHandler(IDocumentDbContext dbContext) : RequestHandler<IngestStateChanged>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(IngestStateChanged request, CancellationToken cancellationToken)
    {
        var file = await _dbContext.FileRefs
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.FileId && candidate.CompanyId == request.CompanyId,
                cancellationToken);

        if (file is null)
        {
            return;
        }

        file.ApplyIngestState(FileIngestState.FromValue(request.State), request.FailedCount, request.ChangedAt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
