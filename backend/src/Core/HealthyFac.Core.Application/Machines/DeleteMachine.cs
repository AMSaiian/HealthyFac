using HealthyFac.Core.Domain.Drafts;
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
/// Removes a machine and its state history, dismisses its pending drafts, and queues the
/// <see cref="MachineDeleted"/> message in the same save.
/// The save interceptor turns each removal into a soft delete, so the rows stay and the query filter hides them.
/// One save stores all removals together. A machine outside the company and a
/// machine that is already deleted, are "not found".
/// </summary>
/// <remarks>
/// The handler loads and dismisses the drafts after it removes the machine. A draft that is tracked when the
/// machine is removed loses its target machine in the fix-up of the removal, and the save then breaks the one-target
/// check of the draft table.
/// </remarks>
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
        var metricDrafts = await _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.TargetMachineId == machineId && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);
        var statusDrafts = await _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.TargetMachineId == machineId && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);

        foreach (var draft in machineDrafts)
        {
            draft.Dismiss();
        }
        foreach (var draft in metricDrafts)
        {
            draft.Dismiss();
        }
        foreach (var draft in statusDrafts)
        {
            draft.Dismiss();
        }
    }
}
