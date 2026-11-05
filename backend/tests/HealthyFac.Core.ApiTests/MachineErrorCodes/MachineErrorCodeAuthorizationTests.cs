using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.MachineErrorCodes.Data;

namespace HealthyFac.Core.ApiTests.MachineErrorCodes;

public sealed class MachineErrorCodeAuthorizationTests(CoreApiFixture fixture) : IClassFixture<CoreApiFixture>
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithoutWritePermission_Returns403()
    {
        var machineId =
            await _fixture.ArrangeMachineAsync(MachineErrorCodeApiTestData.SerialNumberPrefix + "cr-denied");
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            _fixture.ErrorCodesPath(machineId),
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithWritePermission_Returns201WithEncryptedIdLocation()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineErrorCodeApiTestData.SerialNumberPrefix + "cr-ok");
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            _fixture.ErrorCodesPath(machineId),
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.Code));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().StartWith("/" + _fixture.ErrorCodesPath(machineId) + "/");
        body.Should().Contain(response.Headers.Location.ToString().Split('/').Last());
    }

    [Fact]
    public async Task Create_WithSameCodeTwice_Returns409()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineErrorCodeApiTestData.SerialNumberPrefix + "cr-dup");
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);
        await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            _fixture.ErrorCodesPath(machineId),
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.Code));

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            _fixture.ErrorCodesPath(machineId),
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var machineId =
            await _fixture.ArrangeMachineAsync(MachineErrorCodeApiTestData.SerialNumberPrefix + "se-denied");
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            $"{_fixture.ErrorCodesPath(machineId)}/{MachineErrorCodeApiTestData.SearchSegment}",
            claims,
            MachineErrorCodeApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_WithReadPermission_Returns200()
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineErrorCodeApiTestData.SerialNumberPrefix + "se-ok");
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            $"{_fixture.ErrorCodesPath(machineId)}/{MachineErrorCodeApiTestData.SearchSegment}",
            claims,
            MachineErrorCodeApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WithoutReadPermission_Returns403()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("get-denied", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_WithReadPermission_ReturnsErrorCode()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("get-ok", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(errorCode.Id);
    }

    [Fact]
    public async Task Update_WithoutWritePermission_Returns403()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("upd-denied", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithWritePermission_Returns204()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("upd-ok", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims,
            _fixture.CreateErrorCodeBody(MachineErrorCodeApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithoutDeletePermission_Returns403()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("del-denied", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(
            MachineErrorCodeApiTestData.ReadPermission,
            MachineErrorCodeApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithDeletePermission_Returns204()
    {
        var errorCode = await _fixture.ArrangeErrorCodeAsync("del-ok", MachineErrorCodeApiTestData.Code);
        var claims = _fixture.CreateMachineClaims(MachineErrorCodeApiTestData.DeletePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Delete,
            $"{_fixture.ErrorCodesPath(errorCode.MachineId)}/{errorCode.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
