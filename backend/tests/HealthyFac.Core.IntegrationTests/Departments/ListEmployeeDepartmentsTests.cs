using HealthyFac.Core.Application.Departments;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Departments;

[Collection(CoreEmployeesCollection.Name)]
public sealed class ListEmployeeDepartmentsTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ListEmployeeDepartments_Always_ReturnsOnlyActorDepartments()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        using var scope = _fixture.CreateUserScope(
            tenant.CompanyId,
            [tenant.SecondDepartmentId, otherTenant.FirstDepartmentId]);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ListEmployeeDepartmentsRequest, ListEmployeeDepartmentsResponse>>();

        var response = await handler.HandleAsync(
            new ListEmployeeDepartmentsRequest(), TestContext.Current.CancellationToken);

        response.Items.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new DepartmentResponse { Id = tenant.SecondDepartmentId, Name = MachineTestData.SecondDepartmentName });
    }

    [Fact]
    public async Task ListEmployeeDepartments_WithTwoDepartments_SortsByName()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        using var scope = _fixture.CreateUserScope(tenant.CompanyId, tenant.BothDepartmentIds);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ListEmployeeDepartmentsRequest, ListEmployeeDepartmentsResponse>>();

        var response = await handler.HandleAsync(
            new ListEmployeeDepartmentsRequest(), TestContext.Current.CancellationToken);

        response.Items.Select(item => item.Name)
            .Should()
            .Equal(MachineTestData.FirstDepartmentName, MachineTestData.SecondDepartmentName);
    }
}
