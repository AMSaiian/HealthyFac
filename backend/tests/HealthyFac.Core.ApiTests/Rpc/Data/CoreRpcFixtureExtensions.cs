using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Rpc.Data;

public static class CoreRpcFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public async Task<string> ArrangeMachineOfAnotherCompanyAsync()
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(
                ServiceActor.DatabaseInitialization.For(ServiceName.Core));

            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            var company = new Company
            {
                Name = Guid.NewGuid().ToString(format: "N"),
                TimeZoneId = CoreApiTestData.CompanyTimeZoneId,
            };

            dbContext.Companies.Add(company);

            var machine = new Machine(
                company.Id,
                GatewayIds.Create(),
                MachineApiTestData.MachineName,
                Guid.NewGuid().ToString(format: "N"),
                description: null,
                new DateOnly(year: 2099, month: 1, day: 1),
                scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);

            dbContext.Machines.Add(machine);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(machine.Id);
        }
    }
}
