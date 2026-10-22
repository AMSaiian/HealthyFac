using Bogus;
using HealthyFac.Core.Domain.Companies;

namespace HealthyFac.Core.Infrastructure.Companies.Seeding;

internal sealed class CompanyFaker : Faker<Company>
{
    public CompanyFaker(int seed)
    {
        UseSeed(seed);
        RuleFor(company => company.Name, faker => faker.Company.CompanyName());
    }
}
