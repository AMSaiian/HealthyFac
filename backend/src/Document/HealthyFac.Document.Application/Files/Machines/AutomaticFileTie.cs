using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Machines;

/// <summary>
/// Ties an untied attachment to a machine after an ingest found the machine. A user decision after the ingest was
/// queued wins, so the tie applies only when <see cref="FileRef.CanTieAutomatically"/> allows it. Both message
/// handlers share this logic.
/// </summary>
/// <param name="dbContext">Loads the file ref and the registration, and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time of the change.</param>
public sealed class AutomaticFileTie(IDocumentDbContext dbContext, IOutbox outbox, TimeProvider timeProvider)
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Ties the file when the file exists, is an untied attachment, and no user changed the tie after
    /// <paramref name="queuedAt"/>. A deleted or unknown file completes without a change. A concurrent change of the
    /// file makes the save fail, and the inbox retries the message.
    /// </summary>
    /// <param name="fileId">The file to tie.</param>
    /// <param name="companyId">The company of the message.</param>
    /// <param name="machineId">The machine to tie the file to.</param>
    /// <param name="queuedAt">The time when the ingest was queued (UTC).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the tie applies and the machine has no registration at all. <c>MachineCreated</c> may not have
    /// arrived yet, so the inbox retries the message. A deleted registration completes without a change, because no
    /// retry can bring the machine back.
    /// </exception>
    public async Task TieAsync(
        Guid fileId,
        Guid companyId,
        Guid machineId,
        DateTime queuedAt,
        CancellationToken cancellationToken
    )
    {
        var file = await _dbContext.FileRefs
            .SingleOrDefaultAsync(
                candidate => candidate.Id == fileId && candidate.CompanyId == companyId,
                cancellationToken);

        if (file is null || !file.CanTieAutomatically(queuedAt))
        {
            return;
        }

        var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == machineId && candidate.CompanyId == companyId,
                    cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.MachineNotFound);

        if (registration.DeletedAt is not null)
        {
            return;
        }

        var changedAt = _timeProvider.GetUtcNow().UtcDateTime;
        file.TieTo(machineId, changedAt);
        _outbox.Add(
            new FileMachineChanged
            {
                FileId = file.Id,
                CompanyId = file.CompanyId,
                MachineId = machineId,
                ChangedAt = changedAt,
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
