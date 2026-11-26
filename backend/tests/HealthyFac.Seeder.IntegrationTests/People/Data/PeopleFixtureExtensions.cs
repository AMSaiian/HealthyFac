using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.People.Data;

public static class PeopleFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public Task<SeedContext> RunPeopleSeedAsync(CancellationToken cancellationToken) =>
            fixture.RunPartsAsync(current => [SeedRunFixtureExtensions.People(current)], cancellationToken);

        public Task<IReadOnlyList<PlannedEvent>> LoadPeopleEventsAsync()
        {
            var parts = new ISeedPart[] { new PeoplePart(fixture.Hosts, new KeycloakAccounts(fixture.Hosts.Auth)) };
            var folder = new SeedFolder(Path.Combine(AppContext.BaseDirectory, PeopleTestData.SeedFolder));

            return Task.FromResult(SeedPlan.Load(parts, folder));
        }

        public async Task<List<Employee>> LoadEmployeesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Employees
                .AsNoTracking()
                .OrderBy(employee => employee.CreatedAt)
                .ToListAsync();
        }

        public async Task<Company> LoadCompanyAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Companies
                .AsNoTracking()
                .SingleAsync(company => company.Id == PeopleTestData.CompanyId);
        }

        public async Task<List<EmployeeInvite>> LoadInvitesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .EmployeeInvites
                .AsNoTracking()
                .OrderBy(invite => invite.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountShiftTemplateVersionsAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>().ShiftTemplateVersions.CountAsync();
        }

        public async Task<List<ShiftOverride>> LoadOverridesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .ShiftOverrides
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> CountFailedInboxMessagesAsync()
        {
            var failedCount = 0;

            foreach (var host in fixture.Hosts.All)
            {
                var inbox = await fixture.LoadInboxAsync(host);
                failedCount += inbox.Count(message => message.FailedOn is not null || message.Error is not null);
            }

            return failedCount;
        }
    }
}
