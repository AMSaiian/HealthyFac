using Bogus;
using HealthyFac.Core.Domain.Departments;

namespace HealthyFac.Core.Infrastructure.Departments.Seeding;

/// <summary>
/// Generates development departments of one company. Each department takes the next name from
/// <c>names</c>, so the names stay unique within the company, as the unique index on the company and the name
/// requires. Generate at most as many departments as <c>names</c> holds.
/// </summary>
internal sealed class DepartmentFaker : Faker<Department>
{
    /// <summary>
    /// Declares the department shape.
    /// </summary>
    /// <param name="companyId">The identifier of the saved company that owns the departments.</param>
    /// <param name="names">The distinct department names, taken in order.</param>
    /// <param name="timeZoneIds">The IANA time zone IDs to pick from.</param>
    /// <param name="seed">The random seed, so every run produces the same departments.</param>
    public DepartmentFaker(
        Guid companyId,
        IReadOnlyList<string> names,
        IReadOnlyList<string> timeZoneIds,
        int seed
    )
    {
        UseSeed(seed);
        RuleFor(department => department.CompanyId, _ => companyId);
        RuleFor(department => department.Name, faker => names[faker.IndexFaker % names.Count]);
        RuleFor(department => department.TimeZoneId, faker => faker.PickRandom<string>(timeZoneIds));
    }
}
