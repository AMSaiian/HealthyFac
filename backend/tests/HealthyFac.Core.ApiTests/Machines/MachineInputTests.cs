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
    public async Task Create_WithoutExpectedGoLiveOn_Returns400AndStoresNoMachine()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.MachinesPath,
            claims,
            _fixture.CreateCreateBodyWithoutGoLive(MachineApiTestData.NoGoLiveSerialNumber));

        var isStored = await _fixture.HasMachineAsync(MachineApiTestData.NoGoLiveSerialNumber);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        isStored.Should().BeFalse();
    }

    [Fact]
    public async Task Get_AfterCreate_ReturnsExpectedGoLiveOnAsPlainDay()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(MachineApiTestData.GoLiveSerialNumber);
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{MachineApiTestData.MachinesPath}/{encrypted}",
            claims);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(MachineApiTestData.GoLiveJson);
    }

    [Fact]
    public async Task Search_WithGoLiveRangeFromAfterTo_Returns400()
    {
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            claims,
            MachineApiTestData.SearchWithBackwardsGoLiveRangeBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    [Theory]
    [MemberData(nameof(MachineApiTestData.BadStateBodies), MemberType = typeof(MachineApiTestData))]
    public async Task ChangeState_WithMissingOrUnknownState_Returns400(string caseName, string body)
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-state-bad-" + caseName);
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            $"{MachineApiTestData.MachinesPath}/{machineId}{MachineApiTestData.StatePathSuffix}",
            claims,
            body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(MachineApiTestData.BadUpdateStateBodies), MemberType = typeof(MachineApiTestData))]
    public async Task Update_WithMissingOrUnknownState_Returns400(string caseName, string body)
    {
        var machineId = await _fixture.ArrangeMachineAsync(MachineApiTestData.SerialNumber + "-upd-bad-" + caseName);
        var claims = _fixture.CreateMachineClaims(MachineApiTestData.WritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            $"{MachineApiTestData.MachinesPath}/{machineId}",
            claims,
            body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
