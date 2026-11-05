using HealthyFac.Core.Application.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.MachineErrorCodes;

/// <summary>
/// Checks the scope of the machine of an error code. The error code has no company or department, so every error
/// code use case checks the machine in its pre-handler check. This is the only place that holds the machine scope
/// condition.
/// </summary>
public static class MachineErrorCodeScope
{
    extension(ICoreDbContext dbContext)
    {
        /// <summary>
        /// Checks that the machine exists in the company and in one of the departments of the actor. The query filter
        /// of the machines hides a deleted machine.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="departmentIds">The departments of the actor.</param>
        /// <param name="machineId">The machine of the error code.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="NotFoundException">Thrown when the machine is outside the scope or deleted. The answer
        /// is "not found", so it does not show that the machine exists.</exception>
        public async Task EnsureMachineInScopeAsync(
            Guid companyId,
            IReadOnlyCollection<Guid> departmentIds,
            Guid machineId,
            CancellationToken cancellationToken
        )
        {
            var isMachineInScope = await dbContext.Machines.AnyAsync(
                machine => machine.Id == machineId
                    && machine.CompanyId == companyId
                    && departmentIds.Contains(machine.DepartmentId),
                cancellationToken);

            if (!isMachineInScope)
            {
                throw new NotFoundException(MachineErrors.MachineNotFound);
            }
        }
    }
}
