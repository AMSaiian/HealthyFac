namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the scope checks of the actor. Pre-handler checks call them, so every use case checks the company in the
/// same way.
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
    }
}
