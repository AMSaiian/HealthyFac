using HealthyFac.Core.Application.Companies;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts.DaySchedules;

/// <summary>
/// Loads the shift data of an employee, so a use case can resolve the day schedules of a date range.
/// </summary>
/// <param name="dbContext">Reads the versions and the overrides.</param>
/// <param name="companyZoneReader">Reads the time zone of the company.</param>
public sealed class DayScheduleReader(ICoreDbContext dbContext, CompanyZoneReader companyZoneReader)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly CompanyZoneReader _companyZoneReader = companyZoneReader;

    /// <summary>
    /// Reads the time zone of a company.
    /// </summary>
    /// <param name="companyId">The company.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The zone of the company.</returns>
    /// <exception cref="NotFoundException">The company does not exist.</exception>
    public async Task<TimeZoneInfo> GetZoneAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var zone = await _companyZoneReader.FindZoneAsync(companyId, cancellationToken);

        return zone ?? throw new NotFoundException(ShiftErrors.Rejections.EmployeeNotFound);
    }

    /// <summary>
    /// Loads the template versions in force for a date range and the overrides of the range, with their blocks.
    /// </summary>
    /// <param name="employeeId">The employee.</param>
    /// <param name="zone">The zone of the company.</param>
    /// <param name="from">The first date to load.</param>
    /// <param name="to">The last date to load. The caller resolves no date outside the range.</param>
    /// <param name="isTracked">Whether the entities are tracked, so the use case can change them.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The shift data of the employee.</returns>
    public Task<EmployeeShifts> LoadAsync(
        Guid employeeId,
        TimeZoneInfo zone,
        DateOnly from,
        DateOnly to,
        bool isTracked,
        CancellationToken cancellationToken
    ) =>
        LoadCoreAsync(employeeId, zone, from, to, isRangeOnly: true, isTracked, cancellationToken);

    /// <summary>
    /// Loads every template version of the employee and the overrides of a date range, with their blocks. The template
    /// version use cases need the versions outside the range to tell a past version from a missing one.
    /// </summary>
    /// <param name="employeeId">The employee.</param>
    /// <param name="zone">The zone of the company.</param>
    /// <param name="overridesFrom">The first date of the overrides to load.</param>
    /// <param name="overridesTo">The last date of the overrides to load.</param>
    /// <param name="isTracked">Whether the entities are tracked, so the use case can change them.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The shift data of the employee.</returns>
    public Task<EmployeeShifts> LoadWithAllVersionsAsync(
        Guid employeeId,
        TimeZoneInfo zone,
        DateOnly overridesFrom,
        DateOnly overridesTo,
        bool isTracked,
        CancellationToken cancellationToken
    ) =>
        LoadCoreAsync(employeeId, zone, overridesFrom, overridesTo, isRangeOnly: false, isTracked, cancellationToken);

    private async Task<EmployeeShifts> LoadCoreAsync(
        Guid employeeId,
        TimeZoneInfo zone,
        DateOnly from,
        DateOnly to,
        bool isRangeOnly,
        bool isTracked,
        CancellationToken cancellationToken
    )
    {
        var employeeVersions = _dbContext.ShiftTemplateVersions.Where(version => version.EmployeeId == employeeId);

        var versions = (isRangeOnly ? employeeVersions.InForceBetween(from, to) : employeeVersions)
            .Include(version => version.Blocks.OrderBy(block => block.Id))
            .OrderBy(version => version.Id);

        var overrides = _dbContext.ShiftOverrides
            .Include(shiftOverride => shiftOverride.Blocks.OrderBy(block => block.Id))
            .Where(shiftOverride => shiftOverride.EmployeeId == employeeId
                && shiftOverride.Date >= from
                && shiftOverride.Date <= to)
            .OrderBy(shiftOverride => shiftOverride.Id);

        return new EmployeeShifts
        {
            Zone = zone,
            Versions = await (isTracked ? versions : versions.AsNoTracking()).ToListAsync(cancellationToken),
            Overrides = await (isTracked ? overrides : overrides.AsNoTracking()).ToListAsync(cancellationToken),
        };
    }
}
