using Bogus;
using HealthyFac.Core.Domain.Companies;

namespace HealthyFac.Core.Infrastructure.Companies.Seeding;

/// <summary>
/// Generates development companies. Each company takes a random zone of its own list.
/// </summary>
internal sealed class CompanyFaker : Faker<Company>
{
    private static readonly IReadOnlyList<string> TimeZoneIds = ["Europe/Kyiv", "Europe/Berlin", "America/Chicago"];

    /// <summary>
    /// Declares the company shape.
    /// </summary>
    /// <param name="seed">The random seed, so every run produces the same companies.</param>
    public CompanyFaker(int seed)
    {
        UseSeed(seed);
        RuleFor(company => company.Name, faker => faker.Company.CompanyName());
        RuleFor(company => company.TimeZoneId, faker => faker.PickRandom<string>(TimeZoneIds));
    }
}
