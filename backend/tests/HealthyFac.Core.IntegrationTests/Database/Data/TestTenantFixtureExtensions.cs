using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
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
                fixture.InviteTokenService,
                scope.ServiceProvider.GetRequiredService<IOutbox>());

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

        public async Task<IReadOnlyList<Guid>> LoadDepartmentIdsAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Departments
                .Where(department => department.CompanyId == companyId)
                .Select(department => department.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TenantMachine>> LoadTenantMachineAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return await dbContext.Machines
                .AsNoTracking()
                .Where(machine => machine.CompanyId == companyId)
                .Select(machine => new TenantMachine
                {
                    MachineId = machine.Id,
                    DepartmentId = machine.DepartmentId,
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<MachineState>> LoadTenantCurrentStatesAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Machines
                .AsNoTracking()
                .Where(machine => machine.CompanyId == companyId)
                .Select(MachineCurrentState.Selector)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<SeededStateRow>> LoadSeededStateRowsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return await dbContext.MachineStateHistories
                .AsNoTracking()
                .Join(
                    dbContext.Machines,
                    row => row.MachineId,
                    machine => machine.Id,
                    (row, machine) => new SeededStateRow
                    {
                        MachineId = machine.Id,
                        DepartmentId = machine.DepartmentId,
                        MachineName = machine.Name,
                        State = row.State,
                        StartedAt = row.StartedAt,
                    })
                .OrderBy(row => row.StartedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Guid>> SoftDeleteOneStateRowAndLoadAllIdsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var deletedRowId = await dbContext.MachineStateHistories
                .Select(row => row.Id)
                .FirstAsync();
            await dbContext.MachineStateHistories
                .Where(row => row.Id == deletedRowId)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.DeletedAt, DateTime.UtcNow));

            return await dbContext.MachineStateHistories
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Select(row => row.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Guid>> LoadAllStateRowIdsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .MachineStateHistories
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Select(row => row.Id)
                .ToListAsync();
        }

        public async Task<int> CountOutboxMessagesAsync(string messageType)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .OutboxMessages
                .CountAsync(message => message.Type == messageType);
        }
    }
}

public sealed record SeededStateRow
{
    public required Guid MachineId { get; init; }
    public required Guid DepartmentId { get; init; }
    public required string MachineName { get; init; }
    public required MachineState State { get; init; }
    public required DateTime StartedAt { get; init; }
}

public sealed record TenantMachine
{
    public required Guid MachineId { get; init; }
    public required Guid DepartmentId { get; init; }
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
