using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.Core.IntegrationTests.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreSeedingCollection.Name)]
public sealed class EmployeeDepartmentTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task AssignDepartment_WithSameDepartmentFromAnotherQuery_KeepsOneLink()
    {
        await SeedAsync();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var employee = await dbContext.Employees.Include(employee => employee.Departments)
            .FirstAsync(TestContext.Current.CancellationToken);
        var assignedDepartmentId = employee.Departments.First().Id;
        var sameDepartment = await dbContext.Departments
            .AsNoTracking()
            .SingleAsync(department => department.Id == assignedDepartmentId, TestContext.Current.CancellationToken);
        var departmentCount = employee.Departments.Count;

        employee.AssignDepartment(sameDepartment);

        sameDepartment.Should().NotBeSameAs(employee.Departments.First());
        employee.Departments.Should().HaveCount(departmentCount);
    }

    private async Task SeedAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedOptionalDataAsync();
    }
}
