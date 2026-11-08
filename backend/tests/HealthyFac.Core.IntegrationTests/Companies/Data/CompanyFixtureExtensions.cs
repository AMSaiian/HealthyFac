using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Companies.Data;

/// <summary>
/// Arranges companies and runs the current company use case as a user of that company. Every method opens its own DI
/// scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class CompanyFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task SoftDeleteCompanyAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var company = await dbContext.Companies.SingleAsync(candidate => candidate.Id == companyId);

            dbContext.Companies.Remove(company);
            await dbContext.SaveChangesAsync();
        }

        public async Task<GetCurrentCompanyResponse> GetCurrentCompanyAsync(Guid companyId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetCurrentCompanyRequest, GetCurrentCompanyResponse>>();

            return await handler.HandleAsync(new GetCurrentCompanyRequest());
        }
    }
}
