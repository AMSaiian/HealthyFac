using HealthyFac.Core.Application.Employees.SearchEmployees;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class SearchEmployeesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchEmployees_WithEmployeesOfTwoCompanies_ReturnsOnlyOwnCompany()
    {
        var tenant = await ArrangeTenantAsync();

        var response = await _fixture.SearchEmployeesAsync(
            tenant.CompanyId,
            EmployeeSearchTestData.EmployeeRequest(tenant.CompanyId));

        response.TotalCount.Should().Be(expected: 3);
        response.Items.Select(item => item.Email)
            .Should()
            .BeEquivalentTo(
                EmployeeSearchTestData.AnnEmail,
                EmployeeSearchTestData.BobEmail,
                EmployeeSearchTestData.CaraEmail);
    }

    [Fact]
    public async Task SearchEmployees_WithNameFilter_MatchesFirstAndLastNameFromTheStart()
    {
        var tenant = await ArrangeTenantAsync();
        var filter = new EmployeeGridFilter
        {
            Name = EmployeeSearchTestData.Text(
                $"{EmployeeSearchTestData.BobFirstName} {EmployeeSearchTestData.LastName}"),
        };

        var response = await _fixture.SearchEmployeesAsync(
            tenant.CompanyId,
            EmployeeSearchTestData.EmployeeRequest(tenant.CompanyId, filter));

        response.Items.Should().ContainSingle().Which.Email.Should().Be(EmployeeSearchTestData.BobEmail);
    }

    [Fact]
    public async Task SearchEmployees_WithRoleAndStatusFilters_ReturnsMatchingRows()
    {
        var tenant = await ArrangeTenantAsync();
        var filter = new EmployeeGridFilter
        {
            Roles = new SetFilter<RoleCode> { Values = [RoleCode.Supervisor] },
            Statuses = new SetFilter<EmployeeStatus> { Values = [EmployeeStatus.Active] },
        };

        var response = await _fixture.SearchEmployeesAsync(
            tenant.CompanyId,
            EmployeeSearchTestData.EmployeeRequest(tenant.CompanyId, filter));

        response.Items.Should().ContainSingle().Which.Email.Should().Be(EmployeeSearchTestData.AnnEmail);
        response.Items[index: 0].RoleCode.Should().Be(RoleCode.Supervisor);
    }

    [Fact]
    public async Task SearchEmployees_WithEmailFilterAndDescendingNameSort_ReturnsSortedRows()
    {
        var tenant = await ArrangeTenantAsync();
        var filter = new EmployeeGridFilter { Email = EmployeeSearchTestData.Text(value: "ANN.") };
        var allSort = new GridSort<EmployeeSortColumn>
        {
            Column = EmployeeSortColumn.Name,
            Direction = SortDirection.Descending,
        };

        var filtered = await _fixture.SearchEmployeesAsync(
            tenant.CompanyId,
            EmployeeSearchTestData.EmployeeRequest(tenant.CompanyId, filter));
        var sorted = await _fixture.SearchEmployeesAsync(
            tenant.CompanyId,
            EmployeeSearchTestData.EmployeeRequest(tenant.CompanyId, sort: allSort));

        filtered.Items.Should().ContainSingle().Which.Email.Should().Be(EmployeeSearchTestData.AnnEmail);
        sorted.Items.Select(item => item.FirstName)
            .Should()
            .Equal(
                EmployeeSearchTestData.CaraFirstName,
                EmployeeSearchTestData.BobFirstName,
                EmployeeSearchTestData.AnnFirstName);
    }

    private async Task<EmployeeTenant> ArrangeTenantAsync()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var otherCompanyId = await _fixture.CreateCompanyAsync(EmployeeSearchTestData.OtherCompanyName);
        await _fixture.ArrangeEmployeeAsync(
            companyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.AnnEmail,
                FirstName = EmployeeSearchTestData.AnnFirstName,
                LastName = EmployeeSearchTestData.LastName,
                RoleCode = RoleCode.Supervisor,
            });
        await _fixture.ArrangeEmployeeAsync(
            companyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.BobEmail,
                FirstName = EmployeeSearchTestData.BobFirstName,
                LastName = EmployeeSearchTestData.LastName,
            });
        await _fixture.ArrangeEmployeeAsync(
            companyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.CaraEmail,
                FirstName = EmployeeSearchTestData.CaraFirstName,
                LastName = EmployeeSearchTestData.LastName,
                RoleCode = RoleCode.Supervisor,
                IsDeactivated = true,
            });
        await _fixture.ArrangeEmployeeAsync(
            otherCompanyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.OtherEmail,
                FirstName = EmployeeSearchTestData.OtherFirstName,
                LastName = EmployeeSearchTestData.LastName,
            });

        return new EmployeeTenant { CompanyId = companyId };
    }

    private sealed record EmployeeTenant
    {
        public required Guid CompanyId { get; init; }
    }
}
