using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Persistence.SoftDelete;
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
/// Removes a machine, dismisses its pending drafts, and queues the <see cref="MachineDeleted"/> message in one
/// transaction. The save interceptor turns the removal of the machine into a soft delete, so the rows stay and the
/// query filter hides them. The state history has no bound, so the handler does not load it: the soft delete helper
/// soft-deletes it in the transaction of the machine save, and a failed save leaves it unchanged. A machine outside
/// the company and a machine that is already deleted are "not found".
/// </summary>
/// <remarks>
/// The handler loads and dismisses the drafts after it removes the machine. A draft that is tracked when the
/// machine is removed loses its target machine in the fix-up of the removal, and the save then breaks the one-target
/// check of the draft table.
/// </remarks>
/// <param name="dbContext">Loads the machine, soft-deletes the history, and saves the removal.</param>
/// <param name="outbox">Queues the message for the Telemetry service.</param>
/// <param name="timeProvider">Provides the time of the history delete.</param>
/// <param name="currentUser">Provides the actor of the history delete.</param>
public sealed class DeleteMachineRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    ICurrentUserProvider currentUser
) : RequestHandler<DeleteMachineRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override Task HandleInternalAsync(DeleteMachineRequest request, CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(token => DeleteAsync(request, token), cancellationToken);

    private async Task DeleteAsync(DeleteMachineRequest request, CancellationToken cancellationToken)
    {
        var machine = await _dbContext.Machines.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        await _dbContext.MachineStateHistories
            .Where(row => row.MachineId == machine.Id)
            .SoftDeleteAsync(_dbContext, _timeProvider, _currentUser, cancellationToken);
        _dbContext.Machines.Remove(machine);
        await DismissPendingDraftsAsync(machine.Id, cancellationToken);
        _outbox.Add(new MachineDeleted { MachineId = machine.Id, CompanyId = machine.CompanyId });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(MachineErrors.MachineChanged, exception);
        }
    }

    private async Task DismissPendingDraftsAsync(Guid machineId, CancellationToken cancellationToken)
    {
        var machineDrafts = await _dbContext.MachineDrafts
            .Where(draft => draft.TargetMachineId == machineId && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);

        foreach (var draft in machineDrafts)
        {
            draft.Dismiss();
        }
    }
}
