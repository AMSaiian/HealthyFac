using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

public sealed class ChangeMachineStatePreHandlerCheck : PassThroughPreHandlerCheck<MachineStateChanged>;

/// <summary>
/// Stores the machine state that Core sent. A message for an unknown machine throws
/// <see cref="NotFoundException"/>, because the message order is not guaranteed and the inbox retries the message
/// until <c>MachineCreated</c> arrives. A message for a deleted registration changes nothing, and a message whose
/// start is not after the stored start is dropped (<see cref="MachineRegistration.ChangeState"/>).
/// </summary>
/// <remarks>
/// When the machine leaves maintenance, the handler also removes its statistics and streaks and resolves its open
/// episodes at the start of the new state, in the same save (a repair changes the machine, so the old baseline is
/// void). Leaving out of service resets nothing.
/// </remarks>
/// <param name="dbContext">Loads and saves the registration.</param>
/// <param name="outbox">Receives the messages of the resolved episodes.</param>
public sealed class ChangeMachineStateRequestHandler(ITelemetryDbContext dbContext, IOutbox outbox)
    : RequestHandler<MachineStateChanged>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(MachineStateChanged request, CancellationToken cancellationToken)
    {
        var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.RegistrationNotFound);

        if (registration.DeletedAt is not null)
        {
            return;
        }

        var previousState = registration.State;

        var isChanged = registration.ChangeState(
            MachineRegistrationState.FromValue(request.State.Value),
            request.StartedAt);

        if (isChanged)
        {
            if (previousState == MachineRegistrationState.InMaintenance
                && registration.State == MachineRegistrationState.Operational)
            {
                await _dbContext.ResetMachineAsync(_outbox, registration, request.StartedAt, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
