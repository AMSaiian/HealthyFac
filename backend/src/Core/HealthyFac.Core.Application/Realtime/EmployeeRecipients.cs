using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// Finds the web users behind employees. A real-time push goes to a user, and only an employee with an identity user
/// can sign in to the web app.
/// </summary>
public static class EmployeeRecipients
{
    extension(ICoreDbContext dbContext)
    {
        /// <summary>
        /// Maps each employee that has an identity user to that user, in one query. An employee without an identity
        /// user has no entry.
        /// </summary>
        /// <param name="employeeIds">The employees to look up.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The <c>IdentityUserId</c> of each employee, keyed by the employee ID.</returns>
        public async Task<IReadOnlyDictionary<Guid, Guid>> FindIdentityUserIdsAsync(
            IReadOnlyCollection<Guid> employeeIds,
            CancellationToken cancellationToken
        )
        {
            var rows = await dbContext.Employees
                .AsNoTracking()
                .Where(employee => employeeIds.Contains(employee.Id) && employee.IdentityUserId != null)
                .Select(employee => new { employee.Id, IdentityUserId = employee.IdentityUserId!.Value })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(row => row.Id, row => row.IdentityUserId);
        }

        /// <summary>
        /// Finds the identity user of one employee. Call it inside the transaction operation of a use case, because
        /// a read is safe to repeat, and return the result with the operation result. A failed read then fails the
        /// unit of work before the commit, never after it.
        /// </summary>
        /// <param name="employeeId">The employee to look up.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The <c>IdentityUserId</c>, or <see langword="null"/> when the employee has no identity user.
        /// </returns>
        public async Task<Guid?> FindIdentityUserIdAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            var recipients = await dbContext.FindIdentityUserIdsAsync([employeeId], cancellationToken);

            return recipients.TryGetValue(employeeId, out var identityUserId) ? identityUserId : null;
        }
    }
}
