using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Checks the company scope of the employee that a shift use case names.
/// </summary>
public static class ShiftEmployeeScope
{
    extension(ICoreDbContext dbContext)
    {
        /// <summary>
        /// Checks that the employee belongs to the company. An employee of another company is "not found", so the
        /// answer does not show that it exists.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="employeeId">The employee that the request names.</param>
        /// <param name="cancellationToken">Cancels the query.</param>
        /// <returns>A task that completes when the employee is in the company.</returns>
        /// <exception cref="NotFoundException">The employee is not in the company.</exception>
        public async Task EnsureEmployeeInCompanyAsync(
            Guid companyId,
            Guid employeeId,
            CancellationToken cancellationToken
        )
        {
            var isInCompany = await dbContext.Employees
                .AsNoTracking()
                .AnyAsync(
                    employee => employee.Id == employeeId && employee.CompanyId == companyId,
                    cancellationToken);

            if (!isInCompany)
            {
                throw new NotFoundException(ShiftErrors.Rejections.EmployeeNotFound);
            }
        }
    }
}
