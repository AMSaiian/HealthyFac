using Bogus;
using HealthyFac.Core.Domain.Companies;

namespace HealthyFac.Core.Infrastructure.Companies.Seeding;

/// <summary>
/// Generates development companies. Each company takes a random zone from <c>timeZoneIds</c>.
/// </summary>
internal sealed class CompanyFaker : Faker<Company>
{
    /// <summary>
    /// Declares the company shape.
    /// </summary>
    /// <param name="timeZoneIds">The IANA time zone IDs to pick from.</param>
    /// <param name="seed">The random seed, so every run produces the same companies.</param>
    public CompanyFaker(IReadOnlyList<string> timeZoneIds, int seed)
    {
        UseSeed(seed);
        RuleFor(company => company.Name, faker => faker.Company.CompanyName());
        RuleFor(company => company.TimeZoneId, faker => faker.PickRandom<string>(timeZoneIds));
    }
}
