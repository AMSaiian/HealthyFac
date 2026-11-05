using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.MachineErrorCodes.Data;

namespace HealthyFac.Core.ApiTests.MachineErrorCodes;

public sealed class MachineErrorCodeRouteTests(CoreApiFixture fixture) : IClassFixture<CoreApiFixture>
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Get_WithMalformedMachineId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-machine", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{_fixture.ErrorCodesPath(MachineErrorCodeApiTestData.InvalidEncryptedId)}/{errorCode.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithMalformedErrorCodeId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-code", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{MachineErrorCodeApiTestData.InvalidEncryptedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithMalformedMachineId_Returns404()
    {
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            $"{_fixture.ErrorCodesPath(MachineErrorCodeApiTestData.InvalidEncryptedId)}"
            + $"/{MachineErrorCodeApiTestData.SearchSegment}",
            claims,
            MachineErrorCodeApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithMalformedMachineId_Returns404()
    {
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            _fixture.ErrorCodesPath(MachineErrorCodeApiTestData.InvalidEncryptedId),
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithMalformedErrorCodeId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-upd", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{MachineErrorCodeApiTestData.InvalidEncryptedId}",
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithMalformedErrorCodeId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-del", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.DeletePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{MachineErrorCodeApiTestData.InvalidEncryptedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithMalformedMachineId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-upd-machine", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{_fixture.ErrorCodesPath(MachineErrorCodeApiTestData.InvalidEncryptedId)}/{errorCode.Id}",
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithMalformedMachineId_Returns404()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("route-del-machine", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.DeletePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{_fixture.ErrorCodesPath(MachineErrorCodeApiTestData.InvalidEncryptedId)}/{errorCode.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
