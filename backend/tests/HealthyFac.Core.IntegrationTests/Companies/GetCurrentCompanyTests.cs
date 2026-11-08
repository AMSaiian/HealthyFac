using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.IntegrationTests.Companies.Data;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Companies;

[Collection(CoreMachinesCollection.Name)]
public sealed class GetCurrentCompanyTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetCurrentCompany_ForUserOfCompany_ReturnsNameAndTimeZone()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);

        var response = await _fixture.GetCurrentCompanyAsync(companyId);

        response.Name.Should().Be(InviteTestData.CompanyName);
        response.TimeZoneId.Should().Be(InviteTestData.CompanyTimeZoneId);
    }

    [Fact]
    public async Task GetCurrentCompany_ForSoftDeletedCompany_ThrowsNotFound()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        await _fixture.SoftDeleteCompanyAsync(companyId);

        var act = () => _fixture.GetCurrentCompanyAsync(companyId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(CompanyErrors.NotFound);
    }
}
