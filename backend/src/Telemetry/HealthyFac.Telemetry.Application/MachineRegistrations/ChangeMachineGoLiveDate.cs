using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

public sealed class ChangeMachineGoLiveDatePreHandlerCheck : PassThroughPreHandlerCheck<MachineGoLiveDateChanged>;

/// <summary>
/// Stores the go-live date that Core sent. A message for an unknown machine throws
/// <see cref="NotFoundException"/>, because the message order is not guaranteed and the inbox retries the message
/// until <c>MachineCreated</c> arrives. A message for a deleted registration changes nothing, and an older message
/// is dropped (<see cref="MachineRegistration.ChangeGoLive"/>).
/// </summary>
/// <param name="dbContext">Loads and saves the registration.</param>
public sealed class ChangeMachineGoLiveDateRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<MachineGoLiveDateChanged>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        MachineGoLiveDateChanged request,
        CancellationToken cancellationToken
    )
    {
        var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.RegistrationNotFound);

        if (registration.DeletedAt is not null)
        {
            return;
        }

        var isChanged = registration.ChangeGoLive(
            request.ExpectedGoLiveOn,
            request.GoLiveStartsAt,
            request.ChangedAt);

        if (isChanged)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
