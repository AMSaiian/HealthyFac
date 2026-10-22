using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed record SeededDataSnapshot
{
    public required IReadOnlyList<Company> Companies { get; init; }
    public required IReadOnlyList<Department> Departments { get; init; }
    public required IReadOnlyList<Machine> Machines { get; init; }
    public required IReadOnlyList<EmployeeInvite> Invites { get; init; }
    public required IReadOnlyList<Employee> Employees { get; init; }

    public static async Task<SeededDataSnapshot> ReadAsync(CoreDbContext dbContext)
        => new()
        {
            Companies = await dbContext.Companies.AsNoTracking().ToListAsync(),
            Departments = await dbContext.Departments.AsNoTracking().ToListAsync(),
            Machines = await dbContext.Machines.AsNoTracking().ToListAsync(),
            Invites =
                await dbContext.EmployeeInvites.AsNoTracking().Include(invite => invite.Departments).ToListAsync(),
            Employees =
                await dbContext.Employees.AsNoTracking().Include(employee => employee.Departments).ToListAsync(),
        };
}
