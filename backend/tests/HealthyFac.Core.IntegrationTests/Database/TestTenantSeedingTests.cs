using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.Database;

public sealed class TestTenantSeedingTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesCompanyAndActiveSupervisorWithFixedIds()
    {
        await fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var supervisor = await fixture.LoadSeededEmployeeAsync(DatabaseTestData.TestSupervisorEmployeeId);
        var companyCount = await fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var departmentCount = await fixture.CountDepartmentsAsync(DatabaseTestData.TestTenantCompanyId);

        companyCount.Should().Be(1);
        departmentCount.Should().Be(DatabaseTestData.TestTenantDepartmentCount);
        supervisor.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        supervisor.IdentityUserId.Should().Be(DatabaseTestData.TestSupervisorIdentityId);
        supervisor.Email.Should().Be(DatabaseTestData.TestSupervisorEmail);
        supervisor.Status.Should().Be(EmployeeStatus.Active);
        supervisor.RoleCode.Should().Be(RoleCode.Supervisor);
        supervisor.DepartmentCount.Should().Be(DatabaseTestData.TestTenantDepartmentCount);
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_AddsNoEmployeeAcceptedOutboxRow()
    {
        await fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var outboxCount = await fixture.CountOutboxMessagesAsync();

        outboxCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedOptionalData_TwiceWithTestTenant_CreatesOneCompany()
    {
        await fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);
        var companyCountAfterFirstRun = await fixture.CountCompaniesAsync();

        await fixture.RunSeederAsync(DatabaseTestData.TestTenantOptions);

        var tenantCompanyCount = await fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var companyCount = await fixture.CountCompaniesAsync();
        tenantCompanyCount.Should().Be(1);
        companyCount.Should().Be(companyCountAfterFirstRun);
    }
}
