namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the scope checks of the actor. Pre-handler checks call them, so every use case checks the company and the
/// departments in the same way.
/// </summary>
public static class ActorContextScopeChecks
{
    extension(ActorContext actor)
    {
        /// <summary>
        /// Checks that the actor works inside the given company.
        /// </summary>
        /// <param name="companyId">The company that the request names.</param>
        /// <exception cref="ErrorHandling.ForbiddenAccessException">
        /// Thrown when the actor has no company or another company.
        /// </exception>
        public void EnsureCompany(Guid companyId)
        {
            if (actor.CompanyId != companyId)
            {
                throw CurrentUserErrors.CompanyOutOfScope();
            }
        }

        /// <summary>
        /// Checks that the actor belongs to every given department.
        /// </summary>
        /// <param name="departmentIds">The departments that the request names.</param>
        /// <exception cref="ErrorHandling.ForbiddenAccessException">
        /// Thrown when one department is not in <see cref="ActorContext.DepartmentIds"/>.
        /// </exception>
        public void EnsureDepartments(IEnumerable<Guid> departmentIds)
        {
            if (!departmentIds.All(actor.DepartmentIds.Contains))
            {
                throw CurrentUserErrors.DepartmentsOutOfScope();
            }
        }
    }
}
