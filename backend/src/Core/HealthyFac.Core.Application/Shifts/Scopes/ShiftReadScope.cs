using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Checks the scope of a shift read. The company scope reaches every employee of the company. The own scope reaches
/// the employee of the actor only.
/// </summary>
public static class ShiftReadScope
{
    extension(AccessScope scope)
    {
        /// <summary>
        /// Checks that the actor may read the shifts of the employee. An employee outside the scope is "not found", so
        /// the answer does not show that the employee exists.
        /// </summary>
        /// <param name="dbContext">Reads the employees.</param>
        /// <param name="ownEmployee">Finds the employee of the actor.</param>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="employeeId">The employee that the request names.</param>
        /// <param name="cancellationToken">Cancels the queries.</param>
        /// <returns>A task that completes when the read is allowed.</returns>
        /// <exception cref="NotFoundException">The employee is outside the scope.</exception>
        public async Task EnsureEmployeeReadableAsync(
            ICoreDbContext dbContext,
            OwnEmployeeResolver ownEmployee,
            Guid companyId,
            Guid employeeId,
            CancellationToken cancellationToken
        )
        {
            if (scope == AccessScope.Own)
            {
                var ownId = await ownEmployee.FindIdAsync(cancellationToken);
                if (ownId != employeeId)
                {
                    throw new NotFoundException(ShiftErrors.Rejections.EmployeeNotFound);
                }

                return;
            }

            await dbContext.EnsureEmployeeInCompanyAsync(companyId, employeeId, cancellationToken);
        }
    }
}
