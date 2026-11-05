using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record DeleteMachineRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public sealed class DeleteMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DeleteMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Removes a machine, its error codes, and its state history. The save interceptor turns each removal into a soft
/// delete, so the rows stay and the query filter hides them. One save stores all removals together. A machine
/// outside the company or the departments of the actor, and a machine that is already deleted, are "not found".
/// </summary>
/// <param name="dbContext">Loads the machine and saves the removal.</param>
/// <param name="currentUser">Provides the departments of the actor.</param>
public sealed class DeleteMachineRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : RequestHandler<DeleteMachineRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override async Task HandleInternalAsync(
        DeleteMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var departmentIds = _currentUser.GetActor().DepartmentIds.ToList();

        var machine = await _dbContext.Machines.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id
                    && candidate.CompanyId == request.CompanyId
                    && departmentIds.Contains(candidate.DepartmentId),
                cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        var errorCodes = await _dbContext.MachineErrorCodes
            .Where(errorCode => errorCode.MachineId == machine.Id)
            .ToListAsync(cancellationToken);

        var stateHistory = await _dbContext.MachineStateHistories
            .Where(row => row.MachineId == machine.Id)
            .ToListAsync(cancellationToken);

        _dbContext.MachineErrorCodes.RemoveRange(errorCodes);
        _dbContext.MachineStateHistories.RemoveRange(stateHistory);
        _dbContext.Machines.Remove(machine);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
