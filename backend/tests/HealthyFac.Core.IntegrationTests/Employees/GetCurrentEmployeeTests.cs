using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class GetCurrentEmployeeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetCurrentEmployee_WithEmployeeOfTheActor_ReturnsOwnData()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var seed = await _fixture.ArrangeEmployeeAsync(
            companyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.AnnEmail,
                FirstName = EmployeeSearchTestData.AnnFirstName,
                LastName = EmployeeSearchTestData.LastName,
                RoleCode = RoleCode.Supervisor,
            });

        var response = await _fixture.GetCurrentEmployeeAsync(companyId, seed.IdentityUserId);

        response.Id.Should().Be(seed.EmployeeId);
        response.FirstName.Should().Be(EmployeeSearchTestData.AnnFirstName);
        response.LastName.Should().Be(EmployeeSearchTestData.LastName);
        response.RoleCode.Should().Be(RoleCode.Supervisor);
    }

    [Fact]
    public async Task GetCurrentEmployee_WithUserWithoutEmployee_ThrowsNotFound()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);

        var act = () => _fixture.GetCurrentEmployeeAsync(companyId, Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCurrentEmployee_WithEmployeeOfOtherCompany_ThrowsNotFound()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var otherCompanyId = await _fixture.CreateCompanyAsync(EmployeeSearchTestData.OtherCompanyName);
        var seed = await _fixture.ArrangeEmployeeAsync(
            otherCompanyId,
            new EmployeeSeedOptions
            {
                Email = EmployeeSearchTestData.OtherEmail,
                FirstName = EmployeeSearchTestData.OtherFirstName,
                LastName = EmployeeSearchTestData.LastName,
            });

        var act = () => _fixture.GetCurrentEmployeeAsync(companyId, seed.IdentityUserId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
