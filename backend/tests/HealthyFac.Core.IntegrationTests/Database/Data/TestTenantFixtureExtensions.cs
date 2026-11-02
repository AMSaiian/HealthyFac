using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class TestTenantFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task RunSeederAsync(SeedingOptions options)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var seeder = new CoreDevelopmentDataSeeder(
                scope.ServiceProvider.GetRequiredService<CoreDbContext>(),
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                Options.Create(options),
                NullLogger<CoreDevelopmentDataSeeder>.Instance,
                fixture.InviteTokenService);

            await seeder.SeedAsync(CancellationToken.None);
        }

        public async Task<SeededEmployee> LoadSeededEmployeeAsync(Guid employeeId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return await dbContext.Employees
                .AsNoTracking()
                .Where(employee => employee.Id == employeeId)
                .Select(employee => new SeededEmployee
                {
                    Id = employee.Id,
                    CompanyId = employee.CompanyId,
                    IdentityUserId = employee.IdentityUserId,
                    Email = employee.Email,
                    Status = employee.Status,
                    RoleCode = dbContext.Roles
                        .Where(role => role.Id == employee.RoleId)
                        .Select(role => role.Code)
                        .Single(),
                    DepartmentCount = employee.Departments.Count,
                })
                .SingleAsync();
        }

        public async Task<int> CountCompaniesAsync(Guid? companyId = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Companies
                .CountAsync(company => companyId == null || company.Id == companyId);
        }

        public async Task<int> CountDepartmentsAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Departments
                .CountAsync(department => department.CompanyId == companyId);
        }

        public async Task<int> CountOutboxMessagesAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>().OutboxMessages.CountAsync();
        }
    }
}

public sealed record SeededEmployee
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid? IdentityUserId { get; init; }
    public required string Email { get; init; }
    public required EmployeeStatus Status { get; init; }
    public required RoleCode RoleCode { get; init; }
    public required int DepartmentCount { get; init; }
}
