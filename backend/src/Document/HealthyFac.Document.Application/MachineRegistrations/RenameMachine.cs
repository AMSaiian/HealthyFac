using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.MachineRegistrations;

public sealed class RenameMachinePreHandlerCheck : PassThroughPreHandlerCheck<MachineRenamed>;

/// <summary>
/// Stores the new name of a machine that Core renamed. A registration that does not exist or is deleted completes
/// without a change: <c>MachineCreated</c> carries the current name, and a deleted machine needs no name. A rename
/// that is older than the stored one changes nothing, because the message order is not guaranteed.
/// </summary>
/// <param name="dbContext">Loads and saves the registration.</param>
public sealed class RenameMachineRequestHandler(IDocumentDbContext dbContext) : RequestHandler<MachineRenamed>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(MachineRenamed request, CancellationToken cancellationToken)
    {
        var registration = await _dbContext.MachineRegistrations
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.MachineId && candidate.CompanyId == request.CompanyId,
                cancellationToken);

        if (registration is null)
        {
            return;
        }

        registration.Rename(request.Name, request.RenamedAt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
