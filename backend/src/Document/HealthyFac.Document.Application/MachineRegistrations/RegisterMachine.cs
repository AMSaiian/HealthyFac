using EntityFramework.Exceptions.Common;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Domain.MachineRegistrations;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.MachineRegistrations;

public sealed class RegisterMachinePreHandlerCheck : PassThroughPreHandlerCheck<MachineCreated>;

/// <summary>
/// Creates the registration of a machine that Core created. When the registration already exists (for example from
/// the development seed, or after a deleted machine), the handler changes nothing and succeeds.
/// </summary>
/// <remarks>
/// The handler follows the form <b>Resume</b> (rule <b>Cancellation Tokens</b>): it finds the registration first, and
/// creates it only when it is missing. A parallel create of the same machine ends in the unique index of the ID. The
/// handler then reads the registration again and succeeds, because the other writer made the same row.
/// </remarks>
/// <param name="dbContext">Reads and saves the registration.</param>
public sealed class RegisterMachineRequestHandler(IDocumentDbContext dbContext) : RequestHandler<MachineCreated>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(MachineCreated request, CancellationToken cancellationToken)
    {
        var isRegistered = await IsRegisteredAsync(request.MachineId, cancellationToken);

        if (isRegistered)
        {
            return;
        }

        _dbContext.MachineRegistrations.Add(
            new MachineRegistration(request.MachineId, request.CompanyId, request.Name));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            var isRegisteredByOther = await IsRegisteredAsync(request.MachineId, cancellationToken);

            if (!isRegisteredByOther)
            {
                throw;
            }
        }
    }

    private Task<bool> IsRegisteredAsync(Guid machineId, CancellationToken cancellationToken) =>
        _dbContext.MachineRegistrations
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .AnyAsync(registration => registration.Id == machineId, cancellationToken);
}
