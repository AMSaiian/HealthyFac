using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.MachineRegistrations;

public sealed class UnregisterMachinePreHandlerCheck : PassThroughPreHandlerCheck<MachineDeleted>;

/// <summary>
/// Removes the registration of a machine that Core deleted, with the files of the machine. One save stores all
/// removals, and one <see cref="FileDeleted"/> message for each removed file. A registration that does
/// not exist yet makes the handler throw <see cref="NotFoundException"/>, because the message order is not guaranteed
/// and the inbox retries the message until <c>MachineCreated</c> arrives. A registration that is already deleted is
/// done.
/// </summary>
/// <param name="dbContext">Loads and saves the registration.</param>
/// <param name="outbox">Stores the file messages with the removals.</param>
public sealed class UnregisterMachineRequestHandler(IDocumentDbContext dbContext, IOutbox outbox)
    : RequestHandler<MachineDeleted>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(MachineDeleted request, CancellationToken cancellationToken)
    {
        var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.RegistrationNotFound);

        if (registration.DeletedAt is not null)
        {
            return;
        }

        var files = await _dbContext.FileRefs
            .Where(file => file.MachineId == registration.Id)
            .ToListAsync(cancellationToken);

        _dbContext.FileRefs.RemoveRange(files);

        foreach (var file in files)
        {
            _outbox.Add(new FileDeleted { FileId = file.Id, CompanyId = file.CompanyId });
        }

        _dbContext.MachineRegistrations.Remove(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
