using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
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
/// Removes a machine and its state history, and queues the <see cref="MachineDeleted"/> message in the same save.
/// The save interceptor turns each removal into a soft delete, so the rows stay and the query filter hides them.
/// One save stores all removals together. A machine outside the company and a
/// machine that is already deleted, are "not found".
/// </summary>
/// <param name="dbContext">Loads the machine and saves the removal.</param>
/// <param name="outbox">Queues the message for the Telemetry service.</param>
public sealed class DeleteMachineRequestHandler(ICoreDbContext dbContext, IOutbox outbox)
    : RequestHandler<DeleteMachineRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(
        DeleteMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var machine = await _dbContext.Machines.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        var stateHistory = await _dbContext.MachineStateHistories
            .Where(row => row.MachineId == machine.Id)
            .ToListAsync(cancellationToken);

        _dbContext.MachineStateHistories.RemoveRange(stateHistory);
        _dbContext.Machines.Remove(machine);
        _outbox.Add(new MachineDeleted { MachineId = machine.Id, CompanyId = machine.CompanyId });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
