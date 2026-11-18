using HealthyFac.Telemetry.Domain.MachineRegistrations;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

/// <summary>
/// Serializes the handlers that follow or remove the drafts of one machine. A handler locks the registration before it
/// reads it, so a parallel handler of the same machine waits for the end of the first one.
/// </summary>
public interface IMachineRegistrationLock
{
    /// <summary>
    /// Locks the live registration of a machine and loads it tracked. The lock lasts until the transaction of the
    /// message ends. A parallel caller waits for it and then sees the committed registration.
    /// </summary>
    /// <param name="machineId">The ID of the machine.</param>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The registration, or <see langword="null"/> when no live registration exists.</returns>
    Task<MachineRegistration?> LockLiveAsync(Guid machineId, CancellationToken cancellationToken);
}
