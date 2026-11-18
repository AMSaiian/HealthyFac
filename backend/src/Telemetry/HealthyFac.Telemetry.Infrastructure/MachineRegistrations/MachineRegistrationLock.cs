using HealthyFac.Shared.Infrastructure.Persistence.RowLocks;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MachineRegistrations;

namespace HealthyFac.Telemetry.Infrastructure.MachineRegistrations;

/// <summary>
/// Locks the live registration of a machine with <c>FOR UPDATE</c> through the shared row lock. The lock lasts until
/// the inbox transaction of the message ends (<c>InboxMessageRunner</c> runs each message in its own transaction), so
/// the handlers need no <c>RunInTransactionAsync</c>.
/// </summary>
/// <param name="dbContext">Provides the registrations on the connection of the unit of work.</param>
public sealed class MachineRegistrationLock(ITelemetryDbContext dbContext) : IMachineRegistrationLock
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public Task<MachineRegistration?> LockLiveAsync(Guid machineId, CancellationToken cancellationToken) =>
        _dbContext.MachineRegistrations.LockFirstAsync(registration => registration.Id == machineId, cancellationToken);
}
