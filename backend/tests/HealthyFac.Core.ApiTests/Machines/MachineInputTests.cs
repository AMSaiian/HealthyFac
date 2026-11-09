using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Machines;

[Collection(CoreApiCollection.Name)]
public sealed class MachineInputTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Get_WithInvalidEncryptedId_ReturnsNotFoundWithoutLeak()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{MachineApiTestData.MachinesPath}/{MachineApiTestData.InvalidEncryptedId}",
            claims);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().NotContain(MachineApiTestData.StackTraceMarker);
        body.Should().NotContain(MachineApiTestData.InvalidEncryptedId);
    }

    [Fact]
    public async Task Search_WithStateFilterAndSort_BindsEnumerations()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            MachineApiTestData.SearchWithStateFilterBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(MachineApiTestData.BadFilterBodies), MemberType = typeof(MachineApiTestData))]
    public async Task Search_WithBadFilterValue_Returns400(string body)
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithSortOnState_Returns400()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            MachineApiTestData.SearchSortedByStateBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMachine_WithImageSrcInDescription_Returns400()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.MachinesPath,
            claims,
            _fixture.CreateCreateBody(MachineApiTestData.SrcImageDescription));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMachine_WithFileImageInDescription_Returns201()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.MachinesPath,
            claims,
            _fixture.CreateCreateBody(MachineApiTestData.FileImageDescription));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
