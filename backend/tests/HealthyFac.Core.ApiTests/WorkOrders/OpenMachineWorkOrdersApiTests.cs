using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
public sealed class OpenMachineWorkOrdersApiTests(CoreApiFixture fixture)
{
    private const string OpenTitle = "Open order";
    private const string SecondOpenTitle = "Second open order";
    private const string ClosedTitle = "Closed order";
    private const string ForbiddenSerial = "OPEN-FORBIDDEN";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task List_WithOpenAndClosedOrders_ListsOnlyOpenOnesWithoutAssigneeFields()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var machineId = await _fixture.ArrangeMachineAsync(Guid.NewGuid().ToString(format: "N"));
        var openId = await _fixture.CreateMachineOrderAsync(claims, machineId, OpenTitle);
        var employee = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(employee.EmployeeId);
        await _fixture.CreateRejectedMachineOrderAsync(claims, machineId, employee.EncryptedId, ClosedTitle);

        var response = await _fixture.ListOpenOrdersAsync(claims, machineId);

        var items = (await response.Content.ReadFromJsonAsync<JsonArray>(TestContext.Current.CancellationToken))!;
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.Should().ContainSingle();
        items[0]!["id"]!.GetValue<string>().Should().Be(openId);

        items[0]!.AsObject()
            .Select(property => property.Key)
            .Should()
            .BeEquivalentTo("id", "title", "status", "priority", "category", "plannedStartUtc", "plannedEndUtc");
    }

    [Fact]
    public async Task List_WithALimit_ReturnsTheNewestOrdersFirstUpToTheLimit()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var machineId = await _fixture.ArrangeMachineAsync(Guid.NewGuid().ToString(format: "N"));
        await _fixture.CreateMachineOrderAsync(claims, machineId, OpenTitle);
        var secondId = await _fixture.CreateMachineOrderAsync(claims, machineId, SecondOpenTitle);

        var response = await _fixture.ListOpenOrdersAsync(claims, machineId, limit: 1);

        var items = (await response.Content.ReadFromJsonAsync<JsonArray>(TestContext.Current.CancellationToken))!;
        items.Should().ContainSingle();
        items[0]!["id"]!.GetValue<string>().Should().Be(secondId);
    }

    [Fact]
    public async Task List_WithoutALimit_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var machineId = await _fixture.ArrangeMachineAsync(Guid.NewGuid().ToString(format: "N"));

        var response = await _fixture.ListOpenOrdersAsync(claims, machineId, limit: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ForAMachineOfAnotherCompany_Returns404()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var machineId = await _fixture.ArrangeMachineOfAnotherCompanyAsync();

        var response = await _fixture.ListOpenOrdersAsync(claims, machineId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithoutTheReadPermission_Returns403()
    {
        var claims = _fixture.CreateShiftClaims(Guid.CreateVersion7(), CorePermissions.Machines.Read);
        var machineId = await _fixture.ArrangeMachineAsync(ForbiddenSerial);

        var response = await _fixture.ListOpenOrdersAsync(claims, machineId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
