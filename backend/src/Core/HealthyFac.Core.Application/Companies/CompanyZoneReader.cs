using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Companies;

/// <summary>
/// Reads the time zone of a company, so a use case converts a UTC instant to a company day in one way.
/// </summary>
/// <param name="dbContext">Reads the company.</param>
public sealed class CompanyZoneReader(ICoreDbContext dbContext)
{
    private readonly ICoreDbContext _dbContext = dbContext;

    /// <summary>
    /// Reads the time zone of a company.
    /// </summary>
    /// <param name="companyId">The company.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The zone of the company, or <see langword="null"/> when the company does not exist.</returns>
    public async Task<TimeZoneInfo?> FindZoneAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var zoneId = await _dbContext.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        return zoneId is null ? null : TimeZoneInfo.FindSystemTimeZoneById(zoneId);
    }
}
