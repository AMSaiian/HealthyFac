using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class EmployeeDepartmentTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task AssignDepartment_WithSameDepartmentFromAnotherQuery_KeepsOneLink()
    {
        await SeedAsync();
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var employee = await dbContext.Employees.Include(employee => employee.Departments).FirstAsync();
        var assignedDepartmentId = employee.Departments.First().Id;
        var sameDepartment = await dbContext.Departments
            .AsNoTracking()
            .SingleAsync(department => department.Id == assignedDepartmentId);
        var departmentCount = employee.Departments.Count;

        employee.AssignDepartment(sameDepartment);

        sameDepartment.Should().NotBeSameAs(employee.Departments.First());
        employee.Departments.Should().HaveCount(departmentCount);
    }

    private async Task SeedAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>();

        await initializer.SeedRequiredDataAsync();
        await initializer.SeedOptionalDataAsync();
    }
}
