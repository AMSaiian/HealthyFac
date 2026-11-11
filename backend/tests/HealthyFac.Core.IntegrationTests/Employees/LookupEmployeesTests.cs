using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class LookupEmployeesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Lookup_WithEmployeesOfTwoCompaniesAndADeactivatedOne_ReturnsActiveOwnCompanyEmployees()
    {
        var companyId = await _fixture.CreateCompanyAsync(LookupEmployeesTestData.CompanyName);
        var otherCompanyId = await _fixture.CreateCompanyAsync(LookupEmployeesTestData.OtherCompanyName);
        var ann = await ArrangeAsync(
            companyId, LookupEmployeesTestData.AnnEmail, LookupEmployeesTestData.AnnFirstName, isDeactivated: false);
        await ArrangeAsync(
            companyId, LookupEmployeesTestData.BobEmail, LookupEmployeesTestData.BobFirstName, isDeactivated: true);
        await ArrangeAsync(
            otherCompanyId, LookupEmployeesTestData.CaraEmail, LookupEmployeesTestData.CaraFirstName,
            isDeactivated: false);

        var response = await LookupAsync(companyId, search: null);

        response.Items.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new LookupOption
                {
                    Id = ann.EmployeeId,
                    DisplayName = $"{LookupEmployeesTestData.AnnFirstName} {LookupEmployeesTestData.LastName}",
                });
    }

    [Fact]
    public async Task Lookup_WithPartOfTheFullNameInAnotherCase_MatchesFirstAndLastName()
    {
        var companyId = await _fixture.CreateCompanyAsync(LookupEmployeesTestData.CompanyName);
        await ArrangeAsync(
            companyId, LookupEmployeesTestData.AnnEmail, LookupEmployeesTestData.AnnFirstName, isDeactivated: false);
        var cara = await ArrangeAsync(
            companyId, LookupEmployeesTestData.CaraEmail, LookupEmployeesTestData.CaraFirstName, isDeactivated: false);

        var byFirstNamePart = await LookupAsync(companyId, search: "ARA LOP");

        byFirstNamePart.Items.Select(item => item.Id).Should().Equal(cara.EmployeeId);
    }

    private async Task<EmployeeSeed> ArrangeAsync(Guid companyId, string email, string firstName, bool isDeactivated) =>
        await _fixture.ArrangeEmployeeAsync(
            companyId,
            new EmployeeSeedOptions
            {
                Email = email,
                FirstName = firstName,
                LastName = LookupEmployeesTestData.LastName,
                IsDeactivated = isDeactivated,
            });

    private async Task<LookupResponse> LookupAsync(Guid companyId, string? search)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupEmployeesRequest, LookupResponse>>()
            .HandleAsync(new LookupEmployeesRequest { CompanyId = companyId, Search = search });
    }
}
