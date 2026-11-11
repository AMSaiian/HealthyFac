using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Documents;

/// <summary>
/// The message carries the company, and the handler filters every query by it, so no scope check is needed.
/// </summary>
public sealed class ChangeIngestedFileMachinePreHandlerCheck : PassThroughPreHandlerCheck<FileMachineChanged>;

/// <summary>
/// Stores the new machine of a file where Insights holds the machine: on the ingested document when it exists,
/// else on the active attempt, which the worker reads when it stores the document. Both entities ignore a change that
/// is not newer than the stored one, so messages in another order do no harm. The change runs under the lock of the
/// active attempt, so the worker cannot finish between the read and the write.
/// </summary>
/// <param name="dbContext">Loads and changes the document.</param>
/// <param name="attemptLock">Locks the active attempt.</param>
public sealed class ChangeIngestedFileMachineRequestHandler(
    IInsightsDbContext dbContext,
    IIngestAttemptLock attemptLock
)
    : RequestHandler<FileMachineChanged>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IIngestAttemptLock _attemptLock = attemptLock;

    protected override Task HandleInternalAsync(FileMachineChanged request, CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(
            token => ChangeAsync(request, token),
            cancellationToken);

    private async Task ChangeAsync(FileMachineChanged request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptLock.LockActiveAttemptAsync(request.FileId, request.CompanyId, cancellationToken);
        var document = await _dbContext.IngestedDocuments.SingleOrDefaultAsync(
            candidate => candidate.Id == request.FileId && candidate.CompanyId == request.CompanyId,
            cancellationToken);

        if (document is not null)
        {
            document.ChangeMachine(request.MachineId, request.ChangedAt);
        }
        else
        {
            attempt?.ChangeMachine(request.MachineId, request.ChangedAt);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
