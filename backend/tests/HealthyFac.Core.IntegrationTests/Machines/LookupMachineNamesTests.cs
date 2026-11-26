using HealthyFac.Core.Application.Machines.Lookups;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Machines)]
public sealed class LookupMachineNamesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task LookupNames_WithTwoMachinesAndACreateDraft_ReturnsTheirNames()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var first = await _fixture.CreateMachineAsync(
            tenant.CompanyId, name: "First machine", MachineTestData.SerialNumber);

        var second = await _fixture.CreateMachineAsync(
            tenant.CompanyId, name: "Second machine", MachineTestData.OtherSerialNumber);

        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);

        var response = await LookupAsync(tenant.CompanyId, [first, second], [draftId]);

        response.Machines.Select(machine => (machine.Id, machine.Name))
            .Should()
            .BeEquivalentTo([(first, "First machine"), (second, "Second machine")]);

        response.MachineDrafts.Should().ContainSingle().Which.Id.Should().Be(draftId);
        response.MachineDrafts[0].Name.Should().Be(DraftTestData.MachineName);
    }

    [Fact]
    public async Task LookupNames_WithAForeignMachineAndAnUnknownId_LeavesThemOut()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);

        var own = await _fixture.CreateMachineAsync(
            tenant.CompanyId, name: "Own machine", MachineTestData.SerialNumber);

        var foreign = await _fixture.CreateMachineAsync(
            otherTenant.CompanyId, name: "Foreign machine", MachineTestData.OtherSerialNumber);

        var foreignDraft = await _fixture.AddMachineDraftAsync(otherTenant.CompanyId);

        var response = await LookupAsync(
            tenant.CompanyId, [own, foreign, Guid.CreateVersion7()], [foreignDraft, Guid.CreateVersion7()]);

        response.Machines.Should().ContainSingle().Which.Id.Should().Be(own);
        response.MachineDrafts.Should().BeEmpty();
    }

    private async Task<LookupMachineNamesResponse> LookupAsync(
        Guid companyId,
        IReadOnlyList<Guid> machineIds,
        IReadOnlyList<Guid> machineDraftIds
    )
    {
        using var scope = _fixture.CreateUserScope(companyId);

        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupMachineNamesRequest, LookupMachineNamesResponse>>();

        return await handler.HandleAsync(
            new LookupMachineNamesRequest
            {
                CompanyId = companyId,
                MachineIds = machineIds,
                MachineDraftIds = machineDraftIds,
            });
    }
}
