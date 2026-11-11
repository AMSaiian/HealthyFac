using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class LookupMachinesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Lookup_WithMachinesOfTwoCompanies_ReturnsOnlyOwnCompanyByName()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var pressId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, LookupMachinesTestData.PressName, serialNumber: "LK-2");
        var latheId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, LookupMachinesTestData.LatheName, serialNumber: "LK-1");
        await _fixture.CreateMachineAsync(
            otherTenant.CompanyId, LookupMachinesTestData.OtherCompanyMachineName, serialNumber: "LK-3");

        var response = await LookupAsync(tenant.CompanyId, search: null);

        response.Items.Select(item => item.Id).Should().Equal(pressId, latheId);
        response.Items.Select(item => item.DisplayName)
            .Should()
            .Equal(LookupMachinesTestData.PressName, LookupMachinesTestData.LatheName);
    }

    [Fact]
    public async Task Lookup_WithPartOfTheNameInAnotherCase_MatchesInTheMiddleOfTheName()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, LookupMachinesTestData.LatheName, serialNumber: "LK-1");
        var pressId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, LookupMachinesTestData.PressName, serialNumber: "LK-2");

        var response = await LookupAsync(tenant.CompanyId, LookupMachinesTestData.LowerCasePart.ToUpperInvariant());

        response.Items.Select(item => item.Id).Should().Equal(pressId);
    }

    [Fact]
    public async Task Lookup_WithMoreMachinesThanTheLimit_ReturnsTheFirstOnesByName()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        for (var index = 0; index < LookupValidationConstants.MaxItems + 1; index++)
        {
            await _fixture.CreateMachineAsync(
                tenant.CompanyId, $"Machine {index:D3}", $"{LookupMachinesTestData.SerialPrefix}{index}");
        }

        var response = await LookupAsync(tenant.CompanyId, search: null);

        response.Items.Should().HaveCount(LookupValidationConstants.MaxItems);
        response.Items.Select(item => item.DisplayName).Should().BeInAscendingOrder();
        response.Items.Select(item => item.DisplayName).Should().NotContain(unexpected: "Machine 050");
    }

    private async Task<LookupResponse> LookupAsync(Guid companyId, string? search)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupMachinesRequest, LookupResponse>>()
            .HandleAsync(new LookupMachinesRequest { CompanyId = companyId, Search = search });
    }
}
