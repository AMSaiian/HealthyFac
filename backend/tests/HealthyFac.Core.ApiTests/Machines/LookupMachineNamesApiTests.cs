using System.Net;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.Machines;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.Machines)]
public sealed class LookupMachineNamesApiTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Names_WithANullIdList_Returns400()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.NamesPath,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission),
            jsonBody: "{\"machineIds\":null,\"machineDraftIds\":[]}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Names_WithoutDraftReadPermission_Returns403()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.NamesPath,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            jsonBody: "{\"machineIds\":[],\"machineDraftIds\":[]}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
