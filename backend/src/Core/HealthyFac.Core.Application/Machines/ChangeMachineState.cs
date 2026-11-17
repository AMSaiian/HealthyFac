using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record ChangeMachineStateRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required MachineState State { get; init; }
}

public class ChangeMachineStateRequestValidator : AbstractValidator<ChangeMachineStateRequest>
{
    public ChangeMachineStateRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(MachineErrors.MachineIdRequired);

        RuleFor(x => x.State)
            .NotNull()
            .WithMessage(MachineErrors.StateRequired);
    }
}

public sealed class ChangeMachineStatePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ChangeMachineStateRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ChangeMachineStateRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Starts a new state of a machine now, queues <see cref="MachineStateChanged"/> in the same save, and changes nothing
/// else. The same state as the current one changes nothing.
/// A machine outside the company and a deleted machine are "not found".
/// </summary>
/// <remarks>
/// A parallel change of the same machine ends in a conflict, because <see cref="MachineStateChanges.Apply"/> updates
/// the machine row and the row version check catches it. A parallel change with the same instant breaks the history
/// index before the row version check, and it gives the same conflict (<see cref="MachineErrors.MachineChanged"/>).
/// </remarks>
/// <param name="dbContext">Loads the machine with its state history and saves the change.</param>
/// <param name="outbox">Stores the state message with the change.</param>
/// <param name="timeProvider">Provides the start of the new state.</param>
public sealed class ChangeMachineStateRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
)
    : RequestHandler<ChangeMachineStateRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        ChangeMachineStateRequest request,
        CancellationToken cancellationToken
    )
    {
        var machine = await _dbContext.Machines
                .Include(candidate => candidate.StateHistory)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        var isChanged = MachineStateChanges.Apply(
            _dbContext, _outbox, machine, request.State, _timeProvider.GetUtcNow().UtcDateTime);

        if (!isChanged)
        {
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
            when (exception.ConstraintName == MachineStateHistoryIndexNames.MachineStartedAt)
        {
            throw new ConflictException(MachineErrors.MachineChanged, exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(MachineErrors.MachineChanged, exception);
        }
    }
}
