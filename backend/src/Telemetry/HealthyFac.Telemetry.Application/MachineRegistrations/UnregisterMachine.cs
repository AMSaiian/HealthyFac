using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

public sealed class UnregisterMachinePreHandlerCheck : PassThroughPreHandlerCheck<MachineDeleted>;

/// <summary>
/// Removes the registration of a machine that Core deleted, with its operating phases (and their conditions and limit
/// rows), its status definitions, and its metric definitions. The pending and
/// awaiting definition drafts of the machine become dismissed (an applied draft stays). One save stores all changes.
/// A registration that does not exist yet makes the handler throw <see cref="NotFoundException"/>, because the message
/// order is not guaranteed and the inbox retries the message until <c>MachineCreated</c> arrives. A registration that
/// is already deleted is done.
/// </summary>
/// <remarks>
/// The handler locks the live registration first. A parallel follow handler of the same machine then ends before this
/// handler reads the drafts, so no draft stays pending on a deleted machine.
/// </remarks>
/// <param name="dbContext">Loads and saves the registration.</param>
/// <param name="registrationLock">Locks the registration of the machine before the handler reads it.</param>
public sealed class UnregisterMachineRequestHandler(
    ITelemetryDbContext dbContext,
    IMachineRegistrationLock registrationLock
) : RequestHandler<MachineDeleted>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IMachineRegistrationLock _registrationLock = registrationLock;

    protected override async Task HandleInternalAsync(MachineDeleted request, CancellationToken cancellationToken)
    {
        await _registrationLock.LockLiveAsync(request.MachineId, cancellationToken);

        var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.RegistrationNotFound);

        if (registration.DeletedAt is not null)
        {
            return;
        }

        var statusDefinitions = await _dbContext.StatusDefinitions
            .Where(status => status.MachineRegistrationId == registration.Id)
            .ToListAsync(cancellationToken);

        var metricDefinitions = await _dbContext.MetricDefinitions
            .Where(metric => metric.MachineRegistrationId == registration.Id)
            .ToListAsync(cancellationToken);

        var phases = await _dbContext.OperatingPhases
            .Include(phase => phase.Conditions)
            .Include(phase => phase.Limits)
            .Where(phase => phase.MachineRegistrationId == registration.Id)
            .ToListAsync(cancellationToken);

        var metricDrafts = await _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.TargetMachineId == registration.Id)
            .Where(OpenDefinitionDrafts.Predicate<MetricDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var statusDrafts = await _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.TargetMachineId == registration.Id)
            .Where(OpenDefinitionDrafts.Predicate<StatusDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var phaseDrafts = await _dbContext.OperatingPhaseDrafts
            .Where(draft => draft.TargetMachineId == registration.Id)
            .Where(OpenDefinitionDrafts.Predicate<OperatingPhaseDraft>())
            .ToListAsync(cancellationToken);

        metricDrafts.ForEach(draft => draft.Dismiss());
        statusDrafts.ForEach(draft => draft.Dismiss());
        phaseDrafts.ForEach(draft => draft.Dismiss());
        _dbContext.OperatingPhases.RemoveRange(phases);
        _dbContext.StatusDefinitions.RemoveRange(statusDefinitions);
        _dbContext.MetricDefinitions.RemoveRange(metricDefinitions);
        _dbContext.MachineRegistrations.Remove(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
