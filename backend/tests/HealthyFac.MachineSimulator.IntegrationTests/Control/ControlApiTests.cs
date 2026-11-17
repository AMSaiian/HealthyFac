using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HealthyFac.MachineSimulator.IntegrationTests.Control.Data;
using HealthyFac.MachineSimulator.IntegrationTests.Simulation;
using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;
using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.MachineSimulator.IntegrationTests.Control;

[Collection(SimulatorCollection.Name)]
public sealed class ControlApiTests(SimulatorFixture fixture)
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task List_WithTwoMachines_ReturnsThemOrderedByCompanyThenName()
    {
        var companyId = await _fixture.CreateCompanyAsync();
        foreach (var serialNumber in new[]
            { ControlApiTestData.SecondSerialNumber, SimulatorTestData.GenericSerialNumber })
        {
            var machine = await _fixture.CreateMachineAsync(companyId, serialNumber);
            await _fixture.RegisterMachineAsync(machine);
        }

        await _fixture.DiscoverAsync();
        using var client = _fixture.Factory.CreateClient();

        var machines = await client.GetFromJsonAsync<JsonElement>(
            requestUri: "/machines",
            TestContext.Current.CancellationToken);

        machines.EnumerateArray()
            .Select(item => item.GetProperty(propertyName: "serialNumber").GetString())
            .Should()
            .Equal(SimulatorTestData.GenericSerialNumber, ControlApiTestData.SecondSerialNumber);
    }

    [Fact]
    public async Task Get_WithKnownMachine_ReturnsItsFields()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(ScenarioBinding.CncSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var item = await client.GetFromJsonAsync<JsonElement>(
            ControlApiTestData.MachineRoute(machine.MachineId),
            TestContext.Current.CancellationToken);

        item.GetProperty(propertyName: "machineId").GetGuid().Should().Be(machine.MachineId);
        item.GetProperty(propertyName: "companyId").GetGuid().Should().Be(machine.CompanyId);
        item.GetProperty(propertyName: "gatewayId").GetString().Should().Be(machine.GatewayId);
        item.GetProperty(propertyName: "scenario").GetString().Should().Be(expected: "cncTurningCenter");
        item.GetProperty(propertyName: "coreState").GetString().Should().Be(expected: "operational");
        item.GetProperty(propertyName: "isRegistered").GetBoolean().Should().BeTrue();
        item.GetProperty(propertyName: "isLive").GetBoolean().Should().BeTrue();
        item.GetProperty(propertyName: "isRunning").GetBoolean().Should().BeTrue();
        item.GetProperty(propertyName: "isPublishing").GetBoolean().Should().BeTrue();
        item.GetProperty(propertyName: "expectedGoLiveOn").ValueKind.Should().Be(JsonValueKind.String);
        item.GetProperty(propertyName: "counters")
            .GetProperty(SimulatorTestData.CounterKey)
            .GetDouble()
            .Should()
            .Be(expected: 0);
        item.GetProperty(propertyName: "modules")
            .EnumerateArray()
            .Select(module => module.GetProperty(propertyName: "module").GetString())
            .Should()
            .Contain(ControlApiTestData.SpindleModule);
    }

    [Fact]
    public async Task Get_WithUnknownMachine_Returns404()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync(
            ControlApiTestData.MachineRoute(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMode_WithUnknownMachine_Returns404()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(
            client,
            ControlApiTestData.ModeRoute(Guid.NewGuid(), ControlApiTestData.SpindleModule),
            ControlApiTestData.ModeBody(ControlApiTestData.FailureMode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMode_WithUnknownModule_Returns404()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(ScenarioBinding.CncSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(
            client,
            ControlApiTestData.ModeRoute(machine.MachineId, ControlApiTestData.UnknownModule),
            ControlApiTestData.ModeBody(ControlApiTestData.FailureMode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(ControlApiTestData.AllModules), MemberType = typeof(ControlApiTestData))]
    public async Task SetMode_OnGenericMachine_Returns404ForEveryModule(string module)
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(
            client,
            ControlApiTestData.ModeRoute(machine.MachineId, module),
            ControlApiTestData.ModeBody(ControlApiTestData.FailureMode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(ControlApiTestData.InvalidModeBodies), MemberType = typeof(ControlApiTestData))]
    public async Task SetMode_WithInvalidBody_Returns400(string caseName, string body)
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(ScenarioBinding.CncSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(
            client,
            ControlApiTestData.ModeRoute(machine.MachineId, ControlApiTestData.SpindleModule),
            body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
    }

    [Fact]
    public async Task SetMode_WithFailure_Returns204AndTheModeShowsInGet()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(ScenarioBinding.CncSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(
            client,
            ControlApiTestData.ModeRoute(machine.MachineId, ControlApiTestData.SpindleModule),
            ControlApiTestData.ModeBody(ControlApiTestData.FailureMode));

        var item = await client.GetFromJsonAsync<JsonElement>(
            ControlApiTestData.MachineRoute(machine.MachineId),
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        item.GetProperty(propertyName: "modules")
            .EnumerateArray()
            .Single(module =>
                module.GetProperty(propertyName: "module").GetString() == ControlApiTestData.SpindleModule)
            .GetProperty(propertyName: "mode")
            .GetString()
            .Should()
            .Be(ControlApiTestData.FailureMode);
    }

    [Fact]
    public async Task SetRun_WithFalseThenTrue_StopsAndStartsTheMessages()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var stop = await PutAsync(
            client, ControlApiTestData.RunRoute(machine.MachineId), ControlApiTestData.RunBody(isRunning: false));
        var whileStopped = await _fixture.TickExpectingNothingAsync();
        var start = await PutAsync(
            client, ControlApiTestData.RunRoute(machine.MachineId), ControlApiTestData.RunBody(isRunning: true));
        var afterStart = await _fixture.TickAsync();

        stop.StatusCode.Should().Be(HttpStatusCode.NoContent);
        whileStopped.Should().BeEmpty();
        start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterStart.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(ControlApiTestData.InvalidRunBodies), MemberType = typeof(ControlApiTestData))]
    public async Task SetRun_WithInvalidBody_Returns400(string caseName, string body)
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        using var client = _fixture.Factory.CreateClient();

        var response = await PutAsync(client, ControlApiTestData.RunRoute(machine.MachineId), body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
    }

    private static async Task<HttpResponseMessage> PutAsync(HttpClient client, string route, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, mediaType: "application/json");

        return await client.PutAsync(route, content, TestContext.Current.CancellationToken);
    }
}
