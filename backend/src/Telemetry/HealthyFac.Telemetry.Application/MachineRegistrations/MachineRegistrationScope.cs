using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

/// <summary>
/// Checks the scope of the machine of a definition. The definitions have no company, so every
/// definition use case checks the registration of the machine in its pre-handler check. This is the only place that
/// holds the machine scope condition.
/// </summary>
public static class MachineRegistrationScope
{
    extension(ITelemetryDbContext dbContext)
    {
        /// <summary>
        /// Checks that the registration exists in the company of the actor. The query
        /// filter of the registrations hides a deleted machine.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="machineId">The machine of the definition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="NotFoundException">Thrown when the machine is outside the scope, deleted, or unknown. The
        /// answer is "not found", so it does not show that the machine exists.</exception>
        public async Task EnsureMachineInScopeAsync(
            Guid companyId,
            Guid machineId,
            CancellationToken cancellationToken
        )
        {
            var isMachineInScope = await dbContext.MachineRegistrations.AnyAsync(
                registration => registration.Id == machineId
                    && registration.CompanyId == companyId,
                cancellationToken);

            if (!isMachineInScope)
            {
                throw new NotFoundException(MachineRegistrationErrors.MachineNotFound);
            }
        }
    }
}
