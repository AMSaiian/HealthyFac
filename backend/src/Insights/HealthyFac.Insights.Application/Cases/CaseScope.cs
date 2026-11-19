using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>The scope check that every child read of a case shares.</summary>
public static class CaseScope
{
    extension(IInsightsDbContext dbContext)
    {
        /// <summary>
        /// Checks that the case exists in the company. A case of another company is "not found", so the answer does
        /// not show that it exists.
        /// </summary>
        /// <param name="companyId">The company of the user.</param>
        /// <param name="caseId">The case.</param>
        /// <param name="cancellationToken">The token that cancels the query.</param>
        /// <exception cref="NotFoundException">
        /// Thrown with <see cref="CaseErrors.CaseNotFound"/> when the case does not exist in the company.
        /// </exception>
        public async Task EnsureCaseInScopeAsync(
            Guid companyId,
            Guid caseId,
            CancellationToken cancellationToken
        )
        {
            var isCaseFound = await dbContext.MaintenanceCases
                .AsNoTracking()
                .AnyAsync(candidate => candidate.Id == caseId && candidate.CompanyId == companyId, cancellationToken);

            if (!isCaseFound)
            {
                throw new NotFoundException(CaseErrors.CaseNotFound);
            }
        }
    }
}
