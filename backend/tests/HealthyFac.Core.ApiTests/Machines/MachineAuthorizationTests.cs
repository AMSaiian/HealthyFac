using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Machines;

[Collection(CoreApiCollection.Name)]
public sealed class MachineAuthorizationTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            MachineApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_WithReadPermission_Returns200()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            MachineApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_WithoutDeletePermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-del-denied");
        var claims = _fixture.CreateMachineClaims(
            MachineApiTestData.ReadPermission,
            MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithDeletePermission_Returns204()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-del-ok");
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.DeletePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithoutWritePermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-upd-denied");
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims,
            _fixture.CreateUpdateBody(MachineApiTestData.NewSerialNumber + "-denied"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithWritePermission_Returns204()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-upd-ok");
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims,
            _fixture.CreateUpdateBody(MachineApiTestData.NewSerialNumber + "-ok"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_WithReadPermission_ReturnsMachine()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-get-ok");
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(machineId);
    }
}
